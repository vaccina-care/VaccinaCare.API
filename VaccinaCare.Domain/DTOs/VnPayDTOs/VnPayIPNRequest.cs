namespace VaccinaCare.Domain.DTOs.VnPayDTOs;

public class VnPayIPNRequest
{
    public string vnp_TxnRef { get; set; }
    public string vnp_TransactionNo { get; set; }
    public string vnp_ResponseCode { get; set; }
    public string vnp_SecureHash { get; set; }
}