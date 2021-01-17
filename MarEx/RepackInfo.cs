namespace MarEx
{
    public class RepackInfo : ContentInfo
    {
        public string PathAndFileName { get; set; }
        public byte[] File { get; set; }
        public int OrderToc { get; set; }
        public int OrderFile { get; set; }
    }
}
