using System.ComponentModel.DataAnnotations;

namespace WmsElectiveCarePortal.Models
{
    public class UploadModel
    {
        [Display(Name = "Choose a file")]
        [Required(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>Please choose a file")]
        public IFormFile FormFile { get; set; }
        public bool? Result { get; set; }
        public Dictionary<int, List<string>> RowErrors { get; set; }
    }
}
