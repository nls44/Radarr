using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download.Clients.AirDCPP
{
    public class AirDCPPClientSettingsValidator : AbstractValidator<AirDCPPClientSettings>
    {
        public AirDCPPClientSettingsValidator()
        {
        }
    }

    public class AirDCPPClientSettings : IProviderConfig
    {
        private static readonly AirDCPPClientSettingsValidator Validator = new AirDCPPClientSettingsValidator();

        public AirDCPPClientSettings()
        {
            BaseUrl = "http://localhost:5600/api/v1";
        }

        [FieldDefinition(0, Label = "Host", Type = FieldType.Textbox)]
        public string Host { get; set; }

        [FieldDefinition(1, Label = "API base URL")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "Username")]
        public string Username { get; set; }

        [FieldDefinition(3, Type = FieldType.Password, Label = "Password")]
        public string Password { get; set; }

        [FieldDefinition(4, Label = "Default download directory")]
        public string DownloadDirectory { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
