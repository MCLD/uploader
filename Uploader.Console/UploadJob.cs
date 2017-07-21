namespace Uploader.Console
{
    public class UploadJob
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string Site { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string EmailTo { get; set; }
        public string EmailBcc { get; set; }
        public string EmailSubject { get; set; }
        public string EmailBody { get; set; }
        public bool ZipBeforeUpload { get; set; }
    }
}
