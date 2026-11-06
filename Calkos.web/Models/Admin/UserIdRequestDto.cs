namespace Calkos.web.Models.Admin
{
    // DTO condiviso per le richieste AJAX di reset password / blocco / sblocco
    // dall'area Admin. Tutte e tre le operazioni hanno bisogno solo dello UserId.
    public class UserIdRequestDto
    {
        public string UserId { get; set; }
    }
}