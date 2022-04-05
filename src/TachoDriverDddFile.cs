using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using DataFileReader;

public class TachoDriverDddFile
{
    #region Header Types For DDD File Identification


    /// <summary>
    /// A card number as defined by definition (g).
    /// CardNumber ::= CHOICE {
    /// SEQUENCE {
    /// driverIdentification IA5String(SIZE(14)),
    /// cardReplacementIndex CardReplacementIndex,
    /// cardRenewalIndex CardRenewalIndex
    /// },
    ///  ECE/TRANS/SC.1/2006/2/Add.1 page 70  
    ///  SEQUENCE {
    ///  ownerIdentification IA5String(SIZE(13)),
    ///  cardConsecutiveIndex CardConsecutiveIndex,
    ///  cardReplacementIndex CardReplacementIndex,
    ///  cardRenewalIndex CardRenewalIndex
    ///  }
    /// }
    /// driverIdentification is the unique identification of a driver in a Contracting Party.
    /// ownerIdentification is the unique identification of a company or a workshop or a control body within a Contracting Party. 
    /// cardConsecutiveIndex is the card consecutive index.
    /// cardReplacementIndex is the card replacement index.
    /// cardRenewalIndex is the card renewal index.
    /// The first sequence of the choice is suitable to code a driver card number, the second sequence
    /// of the choice is suitable to code workshop, control, and company card numbers.
    ///
    /// All together it is 16 symbols string
    /// </summary>
    public record CardNumber(string DriverIdentification, char ReplacementIndex, char RenewalIndex);


    /// <summary>
    /// DriverCardHolderIdentification
    /// Information, stored in a driver card, related to the identification of the cardholder
    /// (requirement 195).
    /// DriverCardHolderIdentification ::= SEQUENCE {
    /// cardHolderName HolderName,
    /// cardHolderBirthDate Datef,
    /// cardHolderPreferredLanguage Language
    /// }
    /// cardHolderName is the name and first name(s) of the holder of the Driver Card.
    /// cardHolderBirthDate is the date of birth of the holder of the Driver Card.
    /// cardHolderPreferredLanguage is the preferred language of the card holder.
    /// </summary>
    public record DriverCardHolderIdentification(string Surname,
        string FirstNames, DateTime CardHolderBirthDate, string CardHolderPreferredLanguage);
    
    /// <summary>
    /// CardIdentification ::= SEQUENCE {
    /// CardIssuingMemberState NationNumeric,
    /// cardNumber CardNumber,
    /// cardIssuingAuthorityName Name,
    /// cardIssueDate Time Real,
    /// cardValidityBegin Time Real,
    /// cardExpiryDate Time Real
    /// }
    ///
    /// cardIssuingMemberState is the code of the Contracting Party issuing the card.
    /// cardNumber is the card number of the card.
    /// cardIssuingAuthorityName is the name of the authority having issued the Card.
    /// cardIssueDate is the issue date of the Card to the current holder.
    /// cardValidityBegin is the first date of validity of the card.
    /// cardExpiryDate is the date when the validity of the card ends.
        /// </summary>
    [Description("File: EF Identification. File Id: 0520")]
    public record DriverCardIdentification(
        byte CardIssuingMemberState,
        CardNumber CardNumber,
        string CardIssuingAuthorityName,
        DateTime CardIssueDate,
        DateTime CardValidityBegin,
        DateTime CardExpiryDate,
        DriverCardHolderIdentification Driver);

    /// <summary>
    /// 2.14 CardDrivingLicenceInformation
    /// Information, stored in a driver card, related to the card holder driver licence data (requirement
    /// 196).
    /// CardDrivingLicenceInformation ::= SEQUENCE {
    /// drivingLicenceIssuingAuthority Name,
    /// drivingLicenceIssuingNation NationNumeric,
    /// drivingLicenceNumber IA5String(SIZE(16))
    /// }
    /// drivingLicenceIssuingAuthority is the authority responsible for issuing the driving licence.
    /// drivingLicenceIssuingNation is the nationality of the authority that issued the driving licence.
    /// drivingLicenceNumber is the number of the driving licence.
    /// </summary>
    [Description("File: EF Driving_Licence_Info. File Id: 0521")]
    public record DriverCardLicenseInfo(
        string IssuingAuthority,
        byte IssuingNation,
        string Number);

    /// <summary>
    /// CardCurrentUse ::= SEQUENCE {
    /// sessionOpenTime TimeReal,
    /// sessionOpenVehicle VehicleRegistrationIdentification
    /// }
    /// sessionOpenTime is the time when the card is inserted for the current usage. This element is
    /// set to zero at card removal.
    /// sessionOpenVehicle is the identification of the currently used vehicle, set at card insertion.
    /// This element is set to zero at card removal.
    /// </summary>
    [Description("File: EF Current_Usage. File Id: 0507")]
    public record DriverCardCurrentUse(
        DateTime SessionOpenTime,
        VehicleRegistrationInfo SessionOpenVehicle);


    /// <summary>
    /// 2.113 VehicleRegistrationIdentification
    /// Identification of a vehicle, unique for Europe (VRN and Contracting Party)
    /// VehicleRegistrationIdentification ::= SEQUENCE {
    /// vehicleRegistrationNation NationNumeric,
    /// vehicleRegistrationNumber VehicleRegistrationNumber
    /// }
    /// vehicleRegistrationNation is the nation where the vehicle is registered.
    /// vehicleRegistrationNumber is the registration number of the vehicle (VRN).
    /// </summary>
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
                ((SimpleStringRegion)driverCardLicenseInfoRegion.ProcessedRegions["DrivingLicenceNumber"])
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