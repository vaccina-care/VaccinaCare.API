namespace VaccinaCare.API.Resolvers;

public class GraphQLErrorFilter : IErrorFilter
{
    public IError OnError(IError error)
    {
        // Log the detailed error
        Console.WriteLine(error.Exception?.ToString() ?? error.Message);
        
        return error.WithMessage(error.Exception?.Message ?? error.Message);
    }
}