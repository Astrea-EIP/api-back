namespace proto_back.Shared.Errors;

/// <summary>
/// The <c>wheelchair_model_id</c> is not in the wheelchair catalog. Returned as a 400.
/// </summary>
public sealed class UnknownWheelchairModelException : ApiException
{
    public UnknownWheelchairModelException(string modelId)
        : base(StatusCodes.Status400BadRequest,
            $"Unknown wheelchair_model_id '{modelId}': use the id of a model from GET /v0/wheelchairs.")
    {
    }
}
