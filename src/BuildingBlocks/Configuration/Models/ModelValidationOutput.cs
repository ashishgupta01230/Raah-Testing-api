namespace HautSphere.BuildingBlocks.Configuration.Models
{
    public class ModelValidationOutput
    {
        public bool IsValid { get; set; }

        public List<ModelValidationModel> Errors { get; set; }

        public ModelValidationOutput()
        {
            IsValid = true;
        }
    }
    public class ModelValidationModel
    {
        public string Name { get; set; }

        public string Error { get; set; }

        public ModelValidationModel(string name, string error)
        {
            Name = name;
            Error = error;
        }
    }
}
