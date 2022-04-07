using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using DataFileReader;
using Microsoft.VisualBasic;

public class TachoDriverDddFile
{
    #region Header Types For DDD File Identification


    /// <summary>
    /// 2.21 CardNumber 
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
    /// 2.50 DriverCardHolderIdentification
    /// Information, stored in a driver card, related to the identification of the cardholder
    /// (requirement 195).
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
    /// 2.20 CardIdentification
    /// Information, stored in a card, related to the identification of the card (requirements 194, 215,
    /// 231, 235).
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
    /// 2.12 CardCurrentUse
    /// Information about the actual usage of the card (requirement 212).
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
        string RegistrationNumber)
    {
        public static VehicleRegistrationInfo ReadFromObject(ContainerRegion vehicleRegion)
        {
            var vehicleRegistrationNation =
                ((UInt8Region)vehicleRegion.ProcessedRegions["VehicleRegistrationNation"]).ToByte();
            var vehicleRegistrationNumber =
                ((SimpleStringRegion)vehicleRegion.ProcessedRegions["VehicleRegistrationNumber"]).Text;

            return new VehicleRegistrationInfo(vehicleRegistrationNation, vehicleRegistrationNumber);
        }
    };
    
    #endregion

    // TODO: make event and others

    #region Events And Activities

    public enum CardEventType : byte
    {
        //Generalevents = =0x0x,
        [Description("ƒополнительно не уточн€етс€")]
        GeneralNoFurtherDetails = 0x00,
        [Description("¬вод недействительной карточки")]
        GeneralInsertionOfANonValidCard = 0x01,
        [Description("Ќесовместимость карточек")]
        GeneralCardConflict = 0x02,
        [Description("Ќестыковка времени")]
        GeneralTimeOverlap = 0x03,
        [Description("”правление без соответствующей карточки")]
        GeneralDrivingWithoutAnAppropriateCard = 0x04,
        [Description("¬вод карточки в процессе управлени€")]
        GeneralCardInsertionWhileDriving = 0x05,
        [Description("ѕоследний сеанс использовани€ карточки завершен неправильно")]
        GeneralLastCardSessionNotCorrectlyClosed = 0x06,
        [Description("ѕревышение скорости")]
        GeneralOverSpeeding = 0x07,
        [Description("ѕрекращение электропитани€")]
        GeneralPowerSupplyInterruption = 0x08,
        [Description("ќшибка данных о движении")]
        GeneralMotionDataError = 0x09,
        //С0AТH to С0FТH RFU,

        //VehicleUnitRelatedSecurityBreachAttemptEvents            =0x1x,
        [Description("ƒополнительно не уточн€етс€")]
        VehicleUnitNoFurtherDetails = 0x10,
        [Description("—бой в аутентификации датчика движени€")]
        VehicleUnitMotionSensorAuthenticationFailure = 0x11,
        [Description("—бой в аутентификации карточки тахографа")]
        VehicleUnitTachographCardAuthenticationFailure = 0x12,
        [Description("Ќесанкционированна€ замена датчика движени€")]
        VehicleUnitUnauthorisedChangeOfMotionSensor = 0x13,
        [Description("ќшибка, указывающа€ на нарушение целостности при вводе данных на карточку")]
        VehicleUnitCardDataInputIntegrityError = 0x14,
        [Description("ќшибка, указывающа€ на нарушение целостности данных пользовател€, записанных в блоке пам€ти")]
        VehicleUnitStoredUserDataIntegrityError = 0x15,
        [Description("¬нутренн€€ ошибка при передаче данных")]
        VehicleUnitInternalDataTransferError = 0x16,
        [Description("Ќесанкционированное вскрытие корпуса")]
        VehicleUnitUnauthorisedCaseOpening = 0x17,
        [Description("Ќарушение целостности аппаратного оборудовани€")]
        VehicleUnitHardwareSabotage = 0x18,
        //С19ТH to С1FТH RFU,

        //SensorRelatedSecurityBreachAttemptEvents                 =0x2x,
        [Description("ƒополнительно не уточн€етс€")]
        SensorNoFurtherDetails = 0x20,
        [Description("—бой в аутентификации")]
        SensorAuthenticationFailure = 0x21,
        [Description("ќшибка, указывающа€ на нарушение целостности сохраненных данных")]
        SensorStoredDataIntegrityError = 0x22,
        [Description("¬нутренн€€ ошибка при передаче данных")]
        SensorInternalDataTransferError = 0x23,
        [Description("Ќесанкционированное вскрытие корпуса")]
        SensorUnauthorisedCaseOpening = 0x24,
        [Description("Ќарушение целостности аппаратного оборудовани€")]
        SensorHardwareSabotage = 0x25,
        //С26ТH to С2FТH RFU,

        //ControlDeviceFaults                                      =0x3x,
        [Description("ƒополнительно не уточн€етс€")]
        ControlDevNoFurtherDetails = 0x30,
        [Description("¬нутренн€€ неисправность Ѕ”")]
        ControlDevVuInternalFault = 0x31,
        [Description("Ќеисправность принтера")]
        ControlDevPrinterFault = 0x32,
        [Description("Ќеисправность диспле€")]
        ControlDevDisplayFault = 0x33,
        [Description("ќшибка при загрузке")]
        ControlDevDownloadingFault = 0x34,
        [Description("Ќеисправность датчика")]
        ControlDevSensorFault = 0x35,
        //С36ТH to С3FТH RFU,
        //CardFaults                                               =0x4x,
        [Description("—бой в работе карточки, дополнительно не уточн€етс€")]
        CardFaultNoFurtherDetails = 0x40,
        //С41ТH to С4FТH RFU,
        //С50ТH to С7FТH RFU,
        //С80ТH to СFFТH по усмотрению изготовител€.
    }

    /// <summary>
    /// 2.16 CardEventRecord
    /// Information, stored in a driver or a workshop card, related to an event associated to the card
    /// holder (requirements 205 and 223).
    /// CardEventRecord ::= SEQUENCE {
    /// eventType EventFaultType,
    /// eventBeginTime TimeReal,
    /// eventEndTime TimeReal,
    /// eventVehicleRegistration Vehicle RegistrationI dentification
    /// }
    /// eventType is the type of the event.
    /// eventBeginTime is the date and time of beginning of event.
    /// eventEndTime is the date and time of end of event.
    /// eventVehicleRegistration is the VRN and registering Contracting Party of vehicle in which
    /// the event happened.
    /// </summary>
    [Description("Represent Events or FaultEvents from Files: EF Events_Data 0502 and EF Faults_Data 0503")]
    public record CardEventRecord(CardEventType EventType,
        DateTimeOffset EventBegin,
        DateTimeOffset EventEndTime, VehicleRegistrationInfo Vehicle);

    public enum CardSlotType : byte
    {
        [Description("DRIVER (¬ќƒ»“≈Ћ№)")]
        Driver = 0x00,
        [Description("CO-DRIVER (¬“ќ–ќ… ¬ќƒ»“≈Ћ№)")]
        CoDriver = 0x01,
    }

    public enum CardDrivingStatus : byte
    {
        [Description("SINGLE (ќƒ»Ќ)")]
        Single = 0x00,
        [Description("CREW (Ё »ѕј∆)")]
        Crew = 0x01,
    }

    /// <summary>
    /// 2.1 ActivityChangeInfo
    /// This data type enables to code, within a two bytes word, a slot status at 00:00 and/or a driver
    /// status at 00:00 and/or changes of activity and/or changes of driving status and/or changes of
    /// card status for a driver or a co-driver. This data type is related to requirements 084, 109a, 199
    /// and 219.
    /// ActivityChangeInfo ::= OCTET STRING (SIZE(2))
    /// </summary>
    [Description("File: EF Driver_Activity_Data. File Id: 0504")]
    public record ActivityChangeInfo(
        CardSlotType Slot,
        CardDrivingStatus DrivingStatus,
        [Description("Driver (or workshop) card status in the relevant slot")]
        bool Inserted,
        Activity Activity,
        [Description("Actual only TimePart")]
        DateTimeOffset Time
   );

    /// <summary>
    /// 2.5 CardActivityDailyRecord
    /// Information, stored in a card, related to the driver activities for a particular calendar day. This
    /// data type is related to requirements 199 and 219.
    /// CardActivityDailyRecord ::= SEQUENCE {
    /// activityPreviousRecordLength INTEGER(0..CardActivityLengthRange),
    /// activityRecordLength INTEGER(0..CardActivityLengthRange),
    /// activityRecordDate TimeReal,
    /// activityDailyPresenceCounter DailyPresenceCounter,
    /// activityDayDistance Distance,
    /// activityChangeInfo SET SIZE(1..1440) OF ActivityChangeInfo
    /// }
    /// activityPreviousRecordLength is the total length in bytes of the previous daily record. The
    /// maximum value is given by the length of the OCTET STRING containing these records (see
    /// CardActivityLengthRange paragraph 3). When this record is the oldest daily record, the value
    /// of activityPreviousRecordLength must be set to 0.
    /// activityRecordLength is the total length in bytes of this record. The maximum value is given
    /// by the length of the OCTET STRING containing these records.
    /// activityRecordDate is the date of the record.
    /// activityDailyPresenceCounter is the daily presence counter for the card this day.
    /// activityDayDistance is the total distance travelled this day.
    /// activityChangeInfo is the set of ActivityChangeInfo data for the driver this day. It may
    /// contain at maximum 1440 values (one activity change per minute). This set always includes
    /// the activityChangeInfo coding the driver status at 00:00.
    /// </summary>
    [Description("File: EF Driver_Activity_Data. File Id: 0504")]
    public record CardActivityDailyRecord(
        DateTimeOffset Day,
        uint DailyPresenceCounter,
        uint DailyDistance,
        List<ActivityChangeInfo> Activities);

    /// <summary>
    /// 2.30 CardVehicleRecord
    /// Information, stored in a driver or workshop card, related to a period of use of a vehicle during
    /// a calendar day (requirements 197 and 217).
    /// CardVehicleRecord ::= SEQUENCE {
    /// vehicleOdometerBegin OdometerShort,
    /// vehicleOdometerEnd OdometerShort,
    /// vehicleFirstUse TimeReal,
    /// vehicleLastUse TimeReal,
    /// vehicleRegistration VehicleRegistrationIdentification,
    /// vuDataBlockCounter VuDataBlockCounter
    /// }
    /// vehicleOdometerBegin is the vehicle odometer value at the beginning of the period of use of
    /// the vehicle.
    /// vehicleOdometerEnd is the vehicle odometer value at the end of the period of use of the
    /// vehicle.
    /// vehicleFirstUse is the date and time of the beginning of the period of use of the vehicle.
    /// vehicleLastUse is the date and time of the end of the period of use of the vehicle.
    /// vehicleRegistration is the VRN and the registering Contracting Party of the vehicle.
    /// vuDataBlockCounter is the value of the VuDataBlockCounter at last extraction of the period
    /// of use of the vehicle.
    /// </summary>
    [Description("File: EF Vehicles_Used. File Id: 0505")]
    public record CardVehicleRecord(
        uint VehicleOdometerBegin,
        uint VehicleOdometerEnd,
        DateTimeOffset VehicleFirstUse,
        DateTimeOffset VehicleLastUse,
        VehicleRegistrationInfo VehicleRegistration,
        uint VuDataBlockCounter
        );
    #endregion

    [Description("File: EF Current_Usage. File Id: 0507")]
    public DriverCardCurrentUse CardCurrentUse { get; set; }
    [Description("File: EF Identification. File Id: 0520")]
    public DriverCardIdentification CardIdentification { get; set; }
    [Description("File: EF Driving_Licence_Info. File Id: 0521")]
    public DriverCardLicenseInfo DriverLicenseInfo { get; set; }
    [Description("Previous Driver Card Download Time. File: EF Card_Download. File Id: 050E")]
    public DateTimeOffset? LastCardDownload { get; set; }

    /// <summary>
    /// Information, stored in a driver or a workshop card, related to a fault associated to the card holder (requirement 208 and 223).
    /// !!! Only with actual EventBeginTime 
    /// Flat Collection of Fault Events from File: EF Faults_Data 0503
    /// </summary>
    public List<CardEventRecord> ActualCardFaultRecords { get; set; } = new();

    /// <summary>
    /// Information, stored in a driver or a workshop card, related to an event associated to the card.
    /// !!! Only with actual EventBeginTime 
    /// Flat Collection of Events from File: EF Events_Data. File Id: 0502
    /// </summary>
    public List<CardEventRecord> ActualCardEventRecords { get; set; } = new();

    /// <summary>
    /// 2.13 CardDriverActivityInformation, stored in a driver or a workshop card, related to the activities of the driver
    /// File: EF Driver_Activity_Data. File Id: 0504
    /// </summary>
    public List<CardActivityDailyRecord> ActualCardActivityDailyRecords { get; set; } = new();

    /// <summary>
    /// 2.31 CardVehiclesUsed
    /// Information, stored in a driver or workshop card, related to the vehicles used by the card
    /// holder (requirements 197 and 217).
    /// File: EF Driver_Activity_Data. File Id: 0505
    /// </summary>
    public List<CardVehicleRecord> ActualVehicleUsedRecords { get; set; } = new();


    public static TachoDriverDddFile BuildDriverFileForHeader(Stream dddFileStream)
    {
        DataFile dcdf = DriverCardDataFile.CreateOptimized();
        dcdf.ProcessOnlySkipFiles = new HashSet<string>()
        {
            "0x050E",
            "0x0520",
            "0x0521",
            "0x0507",
        };
        dcdf.Process(dddFileStream);

        var result = CreateFromDriverCardDataFile(dcdf, true, true);

        return result;
    }

    public static TachoDriverDddFile BuildDriverFileWithValuableData(Stream dddFileStream)
    {
        DataFile dcdf = DriverCardDataFile.CreateOptimized();
        dcdf.ProcessOnlySkipFiles = new HashSet<string>()
        {
            "0x050E",
            "0x0501",
            "0x0520",
            "0x0521",
            "0x0507", 
            "0x0502",
            "0x0503",
            "0x0504",
            "0x0505",
        };
        dcdf.Process(dddFileStream);
        var result = CreateFromDriverCardDataFile(dcdf, false, true);
     
        return result;
    }

    public static TachoDriverDddFile CreateFromDriverCardDataFile(DataFile dcdf, bool onlyIdentityData, bool throwIfHeaderNotExists)
    {
        var result = new TachoDriverDddFile();

        // Reading from EF Identification 0520
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
            throw new InvalidOperationException("¬ файле DDD ќтсутсвует необходимый файл 'Identification'");
        }

        // Reading from EF Driving_Licence_Info 0521
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
                "¬ файле DDD ќтсутсвует необходимый файл 'CardDrivingLicenceInformation'");
        }

        // Reading from EF Current_Usage 0507
        var cardCurrentUseRegion = (ElementaryFileRegion)dcdf.ProcessedRegions.FirstOrDefault(r =>
            r.Name.Equals("CardCurrentUse", StringComparison.InvariantCultureIgnoreCase));
        if (cardCurrentUseRegion != null)
        {
            var sessionOpenTime = ((TimeRealRegion)cardCurrentUseRegion.ProcessedRegions["SessionOpenTime"]).DateTime;
            var sessionOpenVehicle = ((ContainerRegion)cardCurrentUseRegion.ProcessedRegions["SessionOpenVehicle"]);

            result.CardCurrentUse = new DriverCardCurrentUse(sessionOpenTime,
                VehicleRegistrationInfo.ReadFromObject(sessionOpenVehicle));
        }
        else if (throwIfHeaderNotExists)
        {
            throw new InvalidOperationException("¬ файле DDD ќтсутсвует необходимый файл 'CardCurrentUse'");
        }

        // Reading from EF Current_Usage 050E
        var cardDownloadRegion = (ElementaryFileRegion)dcdf.ProcessedRegions.FirstOrDefault(r =>
            r.Name.Equals("CardDownload", StringComparison.InvariantCultureIgnoreCase));
        if (cardDownloadRegion != null)
        {
            var lastCardDownloadTime = ((TimeRealRegion)cardDownloadRegion.ProcessedRegions["LastCardDownload"]).DateTime;
            
            if (lastCardDownloadTime != CustomBinaryReader.DateTime1970)
            {
                result.LastCardDownload = lastCardDownloadTime.ToUniversalTime();
            }
        }
        
        if (onlyIdentityData)
        {
            return result;
        }
        // Reading Events from EF Events_Data 0502
        var eventsDataRegion = (ElementaryFileRegion)dcdf.ProcessedRegions.FirstOrDefault(r =>
            r.Name.Equals("EventsData", StringComparison.InvariantCultureIgnoreCase));

        var cardEventRecords = (RepeatingRegion)eventsDataRegion?.ProcessedRegions.FirstOrDefault(r =>
            r.Key.Equals("CardEventRecords", StringComparison.InvariantCultureIgnoreCase)).Value;

        if (cardEventRecords != null)
        {
            foreach (var region in cardEventRecords.ProcessedRegions)
            {
                var processedRegion = (RepeatingRegion)region;
                foreach (var region1 in processedRegion.ProcessedRegions)
                {
                    var cardEventRecord = (ContainerRegion)region1;
                    var eventBeginTime = ((TimeRealRegion)cardEventRecord.ProcessedRegions["EventBeginTime"]).DateTime;
                    if (eventBeginTime == CustomBinaryReader.DateTime1970)
                        continue;

                    var eventType = (CardEventType)((UInt8Region)cardEventRecord.ProcessedRegions["EventType"]).ToByte();
                    var eventEndTime = ((TimeRealRegion)cardEventRecord.ProcessedRegions["EventEndTime"]).DateTime;


                    var cardEvent = new CardEventRecord(eventType, eventEndTime, eventEndTime,
                        VehicleRegistrationInfo.ReadFromObject(
                            (ContainerRegion)cardEventRecord.ProcessedRegions["VehicleRegistration"]));

                    result.ActualCardEventRecords.Add(cardEvent);
                }
            }
        }

        // Reading FaultEvents from EF Events_Data 0503
        var faultsDataRegion = (ElementaryFileRegion)dcdf.ProcessedRegions.FirstOrDefault(r =>
            r.Name.Equals("FaultsData", StringComparison.InvariantCultureIgnoreCase));

        var cardFaultRecords = (RepeatingRegion)faultsDataRegion?.ProcessedRegions.FirstOrDefault(r =>
            r.Key.Equals("CardFaultRecords", StringComparison.InvariantCultureIgnoreCase)).Value;

        if (cardFaultRecords != null)
        {
            foreach (var region in cardFaultRecords.ProcessedRegions)
            {
                var processedRegion = (RepeatingRegion)region;
                foreach (var region1 in processedRegion.ProcessedRegions)
                {
                    var cardEventRecord = (ContainerRegion)region1;
                    var eventBeginTime = ((TimeRealRegion)cardEventRecord.ProcessedRegions["FaultBeginTime"]).DateTime;
                    if (eventBeginTime == CustomBinaryReader.DateTime1970)
                        continue;

                    var eventType = (CardEventType)((UInt8Region)cardEventRecord.ProcessedRegions["FaultType"]).ToByte();
                    var eventEndTime = ((TimeRealRegion)cardEventRecord.ProcessedRegions["FaultEndTime"]).DateTime;


                    var cardEvent = new CardEventRecord(eventType, eventEndTime, eventEndTime,
                        VehicleRegistrationInfo.ReadFromObject(
                            (ContainerRegion)cardEventRecord.ProcessedRegions["VehicleRegistration"]));

                    result.ActualCardEventRecords.Add(cardEvent);
                }
            }
        }

        // Reading FaultEvents from EF Driver_Activity_Data 0504
        var driverActivityDataRegion = (ElementaryFileRegion)dcdf.ProcessedRegions.FirstOrDefault(r =>
            r.Name.Equals("DriverActivityData", StringComparison.InvariantCultureIgnoreCase));

        var cardDriverActivityRecords = (CyclicalActivityRegion)driverActivityDataRegion?.ProcessedRegions.FirstOrDefault(r =>
            r.Key.Equals("CardDriverActivity", StringComparison.InvariantCultureIgnoreCase)).Value;

        if (cardDriverActivityRecords != null)
        {
            foreach (var region in cardDriverActivityRecords.ProcessedRegions)
            {
                var processedRegion = (DriverCardDailyActivityRegion)region;
                var activities = new List<ActivityChangeInfo>();
                foreach (var cardEventRecord in processedRegion.ProcessedRegions)
                {
                    activities.Add(new ActivityChangeInfo((CardSlotType)cardEventRecord.Slot,
                        (CardDrivingStatus)cardEventRecord.Status,
                        cardEventRecord.Inserted,
                        cardEventRecord.Activity,
                        cardEventRecord.Time
                        ));
                }

                result.ActualCardActivityDailyRecords.Add(
                    new CardActivityDailyRecord(processedRegion.RecordDate.ToUniversalTime(),
                        processedRegion.DailyPresenceCounter,
                        processedRegion.Distance,
                        activities));
            }
        }


        // Reading Events from EF Vehicles_Used 0505
        var cardVehiclesUsedRegion = (ElementaryFileRegion)dcdf.ProcessedRegions.FirstOrDefault(r =>
            r.Name.Equals("CardVehiclesUsed", StringComparison.InvariantCultureIgnoreCase));

        var cardVehicleRecords = (RepeatingRegion)cardVehiclesUsedRegion?.ProcessedRegions.FirstOrDefault(r =>
            r.Key.Equals("CardVehicleRecords", StringComparison.InvariantCultureIgnoreCase)).Value;

        if (cardVehicleRecords != null)
        {
            foreach (var region in cardVehicleRecords.ProcessedRegions)
            {
                var vehicleRecordRegion = (ContainerRegion)region;

                var vehicleFirstUse = ((TimeRealRegion)vehicleRecordRegion.ProcessedRegions["VehicleFirstUse"]).DateTime;
                if (vehicleFirstUse == CustomBinaryReader.DateTime1970)
                    continue;

                var vehicleOdometerBegin = ((UInt24Region)vehicleRecordRegion.ProcessedRegions["VehicleOdometerBegin"]).ToUInt();
                var vehicleOdometerEnd = ((UInt24Region)vehicleRecordRegion.ProcessedRegions["VehicleOdometerEnd"]).ToUInt();
                var vuDataBlockCounter = ((BCDStringRegion)vehicleRecordRegion.ProcessedRegions["VuDataBlockCounter"]).Value;
                var vehicleLastUse = ((TimeRealRegion)vehicleRecordRegion.ProcessedRegions["VehicleLastUse"]).DateTime;

                var cardVehicle = new CardVehicleRecord(vehicleOdometerBegin,
                    vehicleOdometerEnd,
                    vehicleFirstUse,
                    vehicleLastUse,
                    VehicleRegistrationInfo.ReadFromObject(
                        (ContainerRegion)vehicleRecordRegion.ProcessedRegions["VehicleRegistration"]),
                    vuDataBlockCounter);

                result.ActualVehicleUsedRecords.Add(cardVehicle);
            }
        }

        return result;
    }
}