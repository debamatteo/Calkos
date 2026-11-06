using System;
using System.Security.Cryptography;
using System.Text;

namespace Calkos.web.Helpers // ⚠️ verifica il namespace reale della cartella Helpers nel tuo progetto
{
    /// <summary>
    /// Genera password temporanee crittograficamente sicure, da assegnare
    /// in seguito a un reset amministrativo. Non usa System.Random perché
    /// non è adatto a scopi di sicurezza (sequenza prevedibile).
    /// </summary>
    public static class PasswordGenerator
    {
        // Escludiamo caratteri visivamente ambigui (I, O, l, 0, 1) per ridurre
        // errori di trascrizione quando l'admin comunica la password a voce/telefono.
        private const string Maiuscole = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        private const string Minuscole = "abcdefghijkmnpqrstuvwxyz";
        private const string Numeri = "23456789";
        private const string Simboli = "!@#$%&*?";

        public static string Genera(int lunghezza = 12)
        {
            if (lunghezza < 8)
                throw new ArgumentException("Lunghezza minima consigliata: 8 caratteri.", nameof(lunghezza));

            var tuttiICaratteri = Maiuscole + Minuscole + Numeri + Simboli;
            var risultato = new StringBuilder(lunghezza);

            // Garantiamo almeno un carattere per categoria: rende la password
            // valida anche se in futuro la policy in Program.cs verrà irrigidita.
            risultato.Append(EstraiCarattere(Maiuscole));
            risultato.Append(EstraiCarattere(Minuscole));
            risultato.Append(EstraiCarattere(Numeri));
            risultato.Append(EstraiCarattere(Simboli));

            for (int i = risultato.Length; i < lunghezza; i++)
                risultato.Append(EstraiCarattere(tuttiICaratteri));

            return Mescola(risultato.ToString());
        }

        private static char EstraiCarattere(string sorgente) =>
            sorgente[RandomNumberGenerator.GetInt32(sorgente.Length)];

        // Fisher-Yates: evita che i primi 4 caratteri seguano sempre lo
        // schema Maiuscola-Minuscola-Numero-Simbolo.
        private static string Mescola(string input)
        {
            var array = input.ToCharArray();
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }
            return new string(array);
        }
    }
}