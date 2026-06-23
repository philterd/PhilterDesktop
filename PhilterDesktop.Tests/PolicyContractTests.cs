using Phileas.Policy;
using Phileas.Policy.Filters;
using Phileas.Services;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Guards the editor↔redactor contract. The policy editor now builds the Phileas
    /// model and serializes it with <see cref="PolicySerializer"/>; the desktop redactor
    /// deserializes the same JSON. This verifies a policy enabling SSN/Email/Phone
    /// actually redacts those types end-to-end (the old local model silently did not).
    /// </summary>
    public sealed class PolicyContractTests
    {
        [Fact]
        public void EditorStylePolicy_RoundTrips_AndRedactsEnabledTypes()
        {
            // What the editor produces when the user enables SSN, Email, and Phone.
            var policy = new PhileasPolicy
            {
                Name = "contract",
                Identifiers = new Identifiers
                {
                    Ssn = new Ssn(),
                    EmailAddress = new EmailAddress(),
                    PhoneNumber = new PhoneNumber()
                }
            };

            // Editor serializes (System.Text.Json) ...
            string json = PolicySerializer.SerializeToJson(policy);
            // ... desktop redactor deserializes the same JSON.
            PhileasPolicy loaded = PolicySerializer.DeserializeFromJson(json);

            var result = new FilterService().Filter(
                loaded, "ctx", 0,
                "Email john@example.com phone 555-123-4567 ssn 123-45-6789.");

            Assert.DoesNotContain("john@example.com", result.FilteredText);
            Assert.DoesNotContain("555-123-4567", result.FilteredText);
            Assert.DoesNotContain("123-45-6789", result.FilteredText);
        }

        [Fact]
        public void SerializedPolicy_UsesPhileasJsonKeys()
        {
            var policy = new PhileasPolicy
            {
                Name = "keys",
                Identifiers = new Identifiers { Ssn = new Ssn() }
            };

            string json = PolicySerializer.SerializeToJson(policy);

            Assert.Contains("\"ssn\"", json);          // Phileas identifier key
            Assert.Contains("\"identifiers\"", json);
        }
    }
}
