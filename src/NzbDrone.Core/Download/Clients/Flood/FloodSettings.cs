using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download.Clients.Flood
{
    public class FloodSettingsValidator : AbstractValidator<FloodSettings>
    {
        public FloodSettingsValidator()
        {
            RuleFor(c => c.Url).ValidRootUrl();
        }
    }

    public class FloodSettings : IProviderConfig
    {
        private static readonly FloodSettingsValidator Validator = new FloodSettingsValidator();

        public FloodSettings()
        {
            Url = "http://localhost:3000";
            Tag = "radarr";
            StartOnAdd = true;
        }

        [FieldDefinition(0, Label = "URL", Type = FieldType.Textbox, HelpText = "URL to Flood, e.g. http://[host]:[port]")]
        public string Url { get; set; }

        [FieldDefinition(1, Label = "Username", Type = FieldType.Textbox, Privacy = PrivacyLevel.UserName)]
        public string Username { get; set; }

        [FieldDefinition(2, Label = "Password", Type = FieldType.Password, Privacy = PrivacyLevel.Password)]
        public string Password { get; set; }

        [FieldDefinition(3, Label = "Destination", Type = FieldType.Textbox, HelpText = "Manually specify download destination")]
        public string Destination { get; set; }

        [FieldDefinition(4, Label = "Tag", Type = FieldType.Textbox, HelpText = "Adding a tag specific to Radarr avoids conflicts with unrelated downloads, but it's optional")]
        public string Tag { get; set; }

        [FieldDefinition(5, Label = "Start on add", Type = FieldType.Checkbox)]
        public bool StartOnAdd { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
