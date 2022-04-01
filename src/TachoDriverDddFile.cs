using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using DataFileReader;

public class TachoDriverDddFile
{
    #region Header Types For DDD File Identification

    public record CardNumber(string DriverIdentification, char ReplacementIndex, char RenewalIndex);

    public record DriverCardHolderIdentification(string Surname,
        string FirstNames, DateTime CardHolderBirthDate, string CardHolderPreferredLanguage);

    [Description("File: EF Identification. File Id: 0520")]
    public record DriverCardIdentification(
        byte CardIssuingMemberState,
        CardNumber CardNumber,
        string CardIssuingAuthorityName,
        DateTime CardIssueDate,
        DateTime CardValidityBegin,
        DateTime CardExpiryDate,
        DriverCardHolderIdentification Driver);

    [Description("File: EF Driving_Licence_Info. File Id: 0521")]
    public record DriverCardLicenseInfo(
        string IssuingAuthority,
        byte IssuingNation,
        string Number);

    [Description("File: EF Current_Usage. File Id: 0507")]
    public record DriverCardCurrentUse(
        DateTime SessionOpenTime,
        VehicleRegistrationInfo SessionOpenVehicle);

    public record VehicleRegistrationInfo(
        byte RegistrationNation,
        string RegistrationNumber);
    
    #endregion
    
    // TODO: make event and others

    public DriverCardCurrentUse CardCurrentUse { get; set; }
    public DriverCardIdentification CardIdentification { get; set; }
    public DriverCardLicenseInfo DriverLicenseInfo { get; set; }

    public static TachoDriverDddFile BuildDriverFileForHeader(Stream dddFileStream)
    {
        DataFile dcdf = DriverCardDataFile.CreateOptimized();
        dcdf.ProcessOnlySkipFiles = new HashSet<string>()
        {
            "0x0520",
            "0x0521",
            "0x0507"
        };
        dcdf.Process(dddFileStream);

        var result = CreateFromDriverCardDataFile(dcdf, true, true);

        return result;
    }

    public static TachoDriverDddFile CreateFromDriverCardDataFile(DataFile dcdf, bool onlyIdentityData, bool throwIfHeaderNotExists)
    {
        var result = new TachoDriverDddFile();
        var driverCardIdentificationRegion = (ElementaryFileRegion)dcdf.ProcessedRegions.FirstOrDefault(r =>
            r.Name.Equals("Identification", StringComparison.InvariantCultureIgnoreCase));
        if (driverCardIdentificationRegion != null)
        {
            var cardIdentification =
                (ContainerRegion)(driverCardIdentificationRegion.ProcessedRegions["CardIdentification"]);
            var cardIssuingMemberState =
                ((UInt8Region)cardIdentification.ProcessedRegions["CardIssuingMemberState"]).ToByte();
            var cardNumber = (CardNumberRegion)cardIdentification.ProcessedRegions["CardNumber"];
            var cardIssuingAuthorityName =
                ((NameRegion)cardIdentification.ProcessedRegions["CardIssuingAuthorityName"]).Text;
            var cardIssueDate = ((TimeRealRegion)cardIdentification.ProcessedRegions["CardIssueDate"]);
            var cardValidityBegin = ((TimeRealRegion)cardIdentification.ProcessedRegions["CardValidityBegin"]);
            var cardExpiryDate = ((TimeRealRegion)cardIdentification.ProcessedRegions["CardExpiryDate"]);


            var driverCardHolderIdentification =
                (ContainerRegion)(driverCardIdentificationRegion.ProcessedRegions["DriverCardHolderIdentification"]);
            var driverSurName = ((NameRegion)driverCardHolderIdentification.ProcessedRegions["CardHolderSurname"]).Text;
            var driverFirstNames = ((NameRegion)driverCardHolderIdentification.ProcessedRegions["CardHolderFirstNames"])
                .Text;
            var driverBirthDay = ((DatefRegion)driverCardHolderIdentification.ProcessedRegions["CardHolderBirthDate"])
                .Time;
            var driverPrefferedLanguage =
                ((SimpleStringRegion)driverCardHolderIdentification.ProcessedRegions["CardHolderPreferredLanguage"])
                .Text;

            result.CardIdentification = new DriverCardIdentification(cardIssuingMemberState,
                new CardNumber(cardNumber.DriverIdentification, cardNumber.ReplacementIndex[0],
                    cardNumber.RenewalIndex[0]),
                cardIssuingAuthorityName,
                cardIssueDate.DateTime,
                cardValidityBegin.DateTime,
                cardExpiryDate.DateTime,
                new(driverSurName, driverFirstNames, driverBirthDay ?? DateTime.MinValue, driverPrefferedLanguage)
            );
        }
        else if (throwIfHeaderNotExists)
        {
            throw new InvalidOperationException("В файле DDD Отсутсвует необходимый файл 'Identification'");
        }

        var driverCardLicenseInfoRegion = (ElementaryFileRegion)dcdf.ProcessedRegions.FirstOrDefault(r =>
            r.Name.Equals("CardDrivingLicenceInformation", StringComparison.InvariantCultureIgnoreCase));
        if (driverCardLicenseInfoRegion != null)
        {
            var IssuingAuthority =
                ((SimpleStringRegion)driverCardLicenseInfoRegion.ProcessedRegions["DrivingLicenceIssuingAuthority"])
                .Text;
            var IssuingNation =
                ((UInt8Region)driverCardLicenseInfoRegion.ProcessedRegions["DrivingLicenceIssuingNation"]).ToByte();
            var Number =
                ((SimpleStringRegion)driverCardLicenseInfoRegion.ProcessedRegions["DrivingLicenceIssuingAuthority"])
                .Text;

            result.DriverLicenseInfo = new DriverCardLicenseInfo(IssuingAuthority, IssuingNation, Number);
        }
        else if (throwIfHeaderNotExists)
        {
            throw new InvalidOperationException(
                "В файле DDD Отсутсвует необходимый файл 'CardDrivingLicenceInformation'");
        }

        var cardCurrentUseRegion = (ElementaryFileRegion)dcdf.ProcessedRegions.FirstOrDefault(r =>
            r.Name.Equals("CardCurrentUse", StringComparison.InvariantCultureIgnoreCase));
        if (cardCurrentUseRegion != null)
        {
            var sessionOpenTime = ((TimeRealRegion)cardCurrentUseRegion.ProcessedRegions["SessionOpenTime"]).DateTime;
            var sessionOpenVehicle = ((ContainerRegion)cardCurrentUseRegion.ProcessedRegions["SessionOpenVehicle"]);
            var vehicleRegistrationNation =
                ((UInt8Region)sessionOpenVehicle.ProcessedRegions["vehicleRegistrationNation"]).ToByte();
            var vehicleRegistrationNumber =
                ((SimpleStringRegion)sessionOpenVehicle.ProcessedRegions["vehicleRegistrationNumber"]).Text;

            result.CardCurrentUse = new DriverCardCurrentUse(sessionOpenTime,
                new VehicleRegistrationInfo(vehicleRegistrationNation, vehicleRegistrationNumber));
        }
        else if (throwIfHeaderNotExists)
        {
            throw new InvalidOperationException("В файле DDD Отсутсвует необходимый файл 'CardCurrentUse'");
        }

        if (onlyIdentityData)
        {
            return result;
        }

        return result;
    }
}