namespace VSuiteLab.Models.Helpers;

public class StatusResponse<T>
{
    public string Message {get; set;}
    public bool Success {get; set;}
    public T? Value {get; set;}
    
    public static StatusResponse<T> Ok(T Value) =>
    new StatusResponse<T> { Value = Value, Success = true };
    
    public static StatusResponse<T> Error(string Message) =>
    new StatusResponse<T> { Message = Message, Success = false };
}