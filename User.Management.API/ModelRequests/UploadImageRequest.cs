namespace User.Management.API.ModelRequests
{
    public class UploadImageRequest
    {
        public FormFile? Image { get; set; }
        public string? Description { get; set; }

    }
}
