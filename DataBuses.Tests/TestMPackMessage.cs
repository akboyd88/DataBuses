using MessagePack;

namespace Boyd.DataBuses.Tests
{
    [MessagePackObject]
    public class TestMPackMessage
    {
        [Key(0)]
        public int test1 { get; set; }
        [Key(1)]
        public string test2 { get; set;  }
        [Key(2)]
        public double test3 { get; set; }
    }
}