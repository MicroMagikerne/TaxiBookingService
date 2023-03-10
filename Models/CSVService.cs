using TaxabookingService.Models;
namespace TaxabookingService.Models;

public class CSVService
{
    
public List<PlanDTO> ReadCSV(string path)
{
List<PlanDTO> fulllist = new List<PlanDTO>();
var lines = File.ReadAllLines(path);
foreach (var line in lines)
{
    var values = line.Split(',');
  
    var plan = new PlanDTO(values[0],DateTime.Parse(values[1]),values[2],values[3]);
    fulllist.Add(plan);
}
return fulllist;
}

public void AppendCSV(string path, PlanDTO newplan)
{
    File.AppendAllText(path, $"{newplan.Kundenavn},{newplan.Starttidspunkt},{newplan.Startsted},{newplan.Slutsted}" + Environment.NewLine);

}
}
