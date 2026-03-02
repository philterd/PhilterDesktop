using Newtonsoft.Json;
using Philter.Model.Policy.Filters;
using System;
using System.Collections.Generic;

namespace Philter.Model.Policy
{
    public class Identifiers
    {

        [JsonProperty("ner")]
        public Ner Ner { get; set; }

        [JsonProperty("customDictionaries")]
        public List<CustomDictionary> CustomDictionaries { get; set; }

        [JsonProperty("age")]
        public Age Age { get; set; }

        [JsonProperty("creditCard")]
        public CreditCard CreditCard { get; set; }

        [JsonProperty("date")]
        public Date Date { get; set; }

        [JsonProperty("emailAddress")]
        public EmailAddress EmailAddress { get; set; }

        [JsonProperty("identifiers")]
        public List<Identifier> CustomIdentifiers { get; set; }

        [JsonProperty("ipAddress")]
        public IpAddress IpAddress { get; set; }

        [JsonProperty("phoneNumber")]
        public PhoneNumber PhoneNumber { get; set; }

        [JsonProperty("phoneNumberExtension")]
        public PhoneNumberExtension PhoneNumberExtension { get; set; }

        [JsonProperty("ssn")]
        public Ssn Ssn { get; set; }

        [JsonProperty("stateAbbreviation")]
        public StateAbbreviation StateAbbreviation { get; set; }

        [JsonProperty("url")]
        public Url Url { get; set; }

        [JsonProperty("vin")]
        public Vin Vin { get; set; }

        [JsonProperty("zipCode")]
        public ZipCode ZipCode { get; set; }

        [JsonProperty("city")]
        public City City { get; set; }

        [JsonProperty("county")]
        public County County { get; set; }

        [JsonProperty("firstName")]
        public FirstName FirstName { get; set; }

        [JsonProperty("hospitalAbbreviation")]
        public HospitalAbbreviation HospitalAbbreviation { get; set; }

        [JsonProperty("hospital")]
        public Hospital Hospital { get; set; }

        [JsonProperty("state")]
        public State State { get; set; }

        [JsonProperty("surname")]
        public Surname Surname { get; set; }

    }
}
