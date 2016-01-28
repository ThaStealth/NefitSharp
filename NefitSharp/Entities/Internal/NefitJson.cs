namespace NefitSharp.Entities.Internal
{
    class NefitJson<T>
    {
        public string id { get; set; }
        public string type { get; set; }
        public int recordable { get; set; }
        public int writable { get; set; }
        public T value { get; set; }
    }
}