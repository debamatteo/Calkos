namespace Calkos.web.Models.DTO
{
    public class ProvvigioneAgenteSelectItem
    {
        public int IdProvvigione { get; set; }
        public int IdAgente { get; set; }
        public int IdMandatario { get; set; }
        public int? IdCliente { get; set; }
        public decimal Percentuale { get; set; }

        public string DescrizioneAgente { get; set; } = "";
        public string DescrizioneMandatario { get; set; } = "";
        public string? DescrizioneCliente { get; set; }
    }
}

/*
DTO semplice per passare alla View:

- IdProvvigione
- IdAgente
- IdMandatario
- IdCliente (opzionale)
- Percentuale
- Descrizioni per combo / liste

Nessuna logica, solo trasporto dati Controller → View.
*/
