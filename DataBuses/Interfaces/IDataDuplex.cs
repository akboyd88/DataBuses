namespace Boyd.DataBuses.Interfaces
{
    /// <summary>
    /// A data bus that has input and output
    /// </summary>
    public interface IDataDuplex<TDataIn, TDataOut> : IDataIngress<TDataIn>, IDataEgress<TDataOut>
    {
        
    }
}