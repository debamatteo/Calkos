namespace Calkos.web.Models.DTO
{
    public class AgenteSelectItem
    {
        public int Id { get; set; }
        public string Nome { get; set; } = "";
        public decimal? PercentualeDefault { get; set; }
    }
}

/*
 Un DTO è :
una classe semplice
senza logica
che serve a trasportare dati dal Controller alla View

AgenteSelectItem serve solo per passare:

Id
Nome
ProvvigioneDefault
alla View, così puoi generare <option data-prov="X.YY">.
 * */