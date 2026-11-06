namespace CalkosManager.Domain.Interfaces
{
    /// <summary>
    /// Contratto minimo per esporre le date e i flag di ritardo per i 9 mandatari.
    /// •  Spiegazione della logica: Definire un'interfaccia C# minima che racchiuda solo le proprietà relative a date e ritardi.
    /// •  Spiegazione della modifica / Motivo logico: Poiché abbiamo 9 classi di modello distinte(ProspettoCobral, ProspettoDeAngeli, ecc.), creare un'interfaccia permette al calcolatore di operare su qualsiasi modello senza dover duplicare la logica di calcolo 9 volte. 


    /// </summary>
    public interface IRigaConDate
    {
        DateTime? DataConsegnaIpotetica { get; }
        DateTime? DataConsegna { get; }
        DateTime DataInserimento { get; }
        bool EscludiControlloRitardo { get; }

        bool InRitardo { get; set; }
        int? GiorniRitardo { get; set; }
    }
}