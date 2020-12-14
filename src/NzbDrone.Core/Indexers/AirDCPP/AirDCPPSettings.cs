using System.Collections.Generic;
using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.AirDCPP
{
    public class AirDCPPSettingsValidator : AbstractValidator<AirDCPPSettings>
    {
        public AirDCPPSettingsValidator()
        {
            RuleFor(c => c.Username).NotEmpty();
            RuleFor(c => c.Password).NotEmpty();
            RuleFor(c => c.Delay).GreaterThanOrEqualTo(0);
        }
    }

    public class AirDCPPSettings : IIndexerSettings
    {
        private static readonly AirDCPPSettingsValidator Validator = new AirDCPPSettingsValidator();

        public AirDCPPSettings()
        {
            Delay = 3000;
            MultiLanguages = new List<int>();
        }

        [FieldDefinition(0, Label = "API base URL")]
        public string BaseUrl { get; set; }

        [FieldDefinition(1, Label = "Username")]
        public string Username { get; set; }

        [FieldDefinition(2, Type = FieldType.Password, Label = "Password")]
        public string Password { get; set; }

        [FieldDefinition(3, Label = "Delay", HelpText = "Time in milliseconds to wait before retrieving hub search results", Advanced = true)]
        public int Delay { get; set; }
        public IEnumerable<int> MultiLanguages { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
