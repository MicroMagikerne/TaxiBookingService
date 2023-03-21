using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using TaxabookingService.Models;
using System.Text;
using RabbitMQ.Client;
using System.Text.Json;


// Namespace for hele projektet
namespace TaxiBookingService
{
    // Angiv ruten til controlleren og gør den til en API-controller
    [Route("api/")]
    [ApiController]
    public class TaxiBookingController : ControllerBase
    {

    private readonly ILogger<TaxiBookingController> _logger;
    private readonly IModel _channel;
    private string CSVPath = string.Empty;
    private string RHQHN = string.Empty;


    public TaxiBookingController(ILogger<TaxiBookingController> logger, IConfiguration configuration)
    {
        CSVPath = configuration["CSVPath"] ?? string.Empty;
        RHQHN = configuration["RMQHN"] ?? string.Empty;
        _logger = logger;
         // Opret forbindelse til RabbitMQ
        var factory = new ConnectionFactory() { HostName = RHQHN };
        var connection = factory.CreateConnection();
        _channel = connection.CreateModel();
    }

        // Angiver HTTP, og ruten til handlingen, der tilføjer en booking
    [HttpPost("booking")]
public IActionResult AddBooking(PlanDTO bookingDTO, ILogger<TaxiBookingController> logger)
{
    try
    {
        // Kode til at tilføje booking
        var newBooking = new PlanDTO
        {
            Kundenavn = bookingDTO.Kundenavn,
            Starttidspunkt = bookingDTO.Starttidspunkt,
            Startsted = bookingDTO.Startsted,
            Slutsted = bookingDTO.Slutsted
        };

       
        {
            // Declare køen, hvis den ikke allerede findes
            _channel.QueueDeclare(queue: "booking_queue",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

   
            var body = JsonSerializer.SerializeToUtf8Bytes(newBooking);

            // Publicer beskeden til køen
            _channel.BasicPublish(exchange: "",
                                 routingKey: "booking_queue",
                                 mandatory: true,
                                 basicProperties: null,
                                 body: body);
            logger.LogInformation("Booking sent to RabbitMQ");
        }

        return Ok();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error while adding booking");
        return StatusCode(500);
    }
}

        // Angiv HTTP og ruten til handlingen, der genererer en plan over alle bookinger
        [HttpGet("plan")]
        public IActionResult GetPlan()
        {
            try
            {
                // Generer en liste af DTO'er, der repræsenterer planen
                var plan = GeneratePlanDTOs();
                return Ok(plan);
            }
            catch (Exception ex)
            {
                // Hvis der opstår en fejl, returner en HTTP-statuskode 500 (Internal Server Error) og fejlmeddelelsen
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // Metode til at generere en liste af DTO'er, der repræsenterer planen
        private List<PlanDTO> GeneratePlanDTOs()
        {
            // Hent alle bookinger som DTO'er
            var bookings = GetBookingsDTOs();

            // Lav en tom liste af DTO'er, der repræsenterer planen
            List<PlanDTO> OrderedPlanList = bookings.OrderBy(b=> b.Starttidspunkt).ToList();

            // Returnér listen af DTO'er, der repræsenterer planen
            return OrderedPlanList;
        }

        // Metode til at hente alle bookinger som DTO'er
        private List<PlanDTO> GetBookingsDTOs()
        {
            // Kode til at hente bookinger fra CSV
            CSVService csvservice = new CSVService();
            List<PlanDTO> fulllist = new List<PlanDTO>();
            fulllist = csvservice.ReadCSV(CSVPath);

            //tjekker om der blev hentet noget, hvis ikke returnere den noget seeddata
            if(fulllist.Count < 1){
                return new List<PlanDTO>
                {
                    new PlanDTO
                    {
                        Kundenavn = "Kerstine",
                        Starttidspunkt = DateTime.Now,
                        Startsted = "Skovkanten 10",
                        Slutsted = "Viby 5"
                    },
                        new PlanDTO
                    {
                        Kundenavn = "Rikke",
                        Starttidspunkt = DateTime.Now,
                        Startsted = "Frederiks allé 7",
                        Slutsted = "Viby 6"
                    }
                };
            }
            else{
                return fulllist;
            }
        }

// Endepunkt som læser det interne metadata indhold fra jeres .NETassembly og sender det til en REST-klient.
    [HttpGet("version")]
        public IEnumerable<string> Get()
        {
        _logger.LogInformation("Metoden er blevet kaldt WUHUHU tjek git");
        var properties = new List<string>();
        var assembly = typeof(Program).Assembly;
        foreach (var attribute in assembly.GetCustomAttributesData())
        {
        properties.Add($"{attribute.AttributeType.Name} - {attribute.ToString()}");
        }
        return properties;
        }

    }

}