﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace PokemonGo.RocketAPI.Console {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "14.0.0.0")]
    internal sealed partial class UserSettings : global::System.Configuration.ApplicationSettingsBase {
        
        private static UserSettings defaultInstance = ((UserSettings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new UserSettings())));
        
        public static UserSettings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("10")]
        public double DefaultAltitude {
            get {
                return ((double)(this["DefaultAltitude"]));
            }
            set {
                this["DefaultAltitude"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("99")]
        public float TransferPokemonKeepAllAboveIVValue {
            get {
                return ((float)(this["TransferPokemonKeepAllAboveIVValue"]));
            }
            set {
                this["TransferPokemonKeepAllAboveIVValue"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("2500")]
        public int TransferPokemonKeepAllAboveCPValue {
            get {
                return ((int)(this["TransferPokemonKeepAllAboveCPValue"]));
            }
            set {
                this["TransferPokemonKeepAllAboveCPValue"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("10")]
        public double WalkingSpeedInKilometerPerHour {
            get {
                return ((double)(this["WalkingSpeedInKilometerPerHour"]));
            }
            set {
                this["WalkingSpeedInKilometerPerHour"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("4")]
        public double MinRandomWalkingSpeed {
            get {
                return ((double)(this["MinRandomWalkingSpeed"]));
            }
            set {
                this["MinRandomWalkingSpeed"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("8")]
        public double MaxRandomWalkingSpeed {
            get {
                return ((double)(this["MaxRandomWalkingSpeed"]));
            }
            set {
                this["MaxRandomWalkingSpeed"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public double DefaultLatitude {
            get {
                return ((double)(this["DefaultLatitude"]));
            }
            set {
                this["DefaultLatitude"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public double DefaultLongitude {
            get {
                return ((double)(this["DefaultLongitude"]));
            }
            set {
                this["DefaultLongitude"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool UsePokemonToNotCatchList {
            get {
                return ((bool)(this["UsePokemonToNotCatchList"]));
            }
            set {
                this["UsePokemonToNotCatchList"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Google")]
        public string AuthType {
            get {
                return ((string)(this["AuthType"]));
            }
            set {
                this["AuthType"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("PtcUsername")]
        public string PtcUsername {
            get {
                return ((string)(this["PtcUsername"]));
            }
            set {
                this["PtcUsername"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("PtcPassword")]
        public string PtcPassword {
            get {
                return ((string)(this["PtcPassword"]));
            }
            set {
                this["PtcPassword"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool PrioritizeIVOverCP {
            get {
                return ((bool)(this["PrioritizeIVOverCP"]));
            }
            set {
                this["PrioritizeIVOverCP"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1500")]
        public int MaxTravelDistanceInMeters {
            get {
                return ((int)(this["MaxTravelDistanceInMeters"]));
            }
            set {
                this["MaxTravelDistanceInMeters"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool UseGPXPathing {
            get {
                return ((bool)(this["UseGPXPathing"]));
            }
            set {
                this["UseGPXPathing"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("GPXFile.gpx")]
        public string GPXFile {
            get {
                return ((string)(this["GPXFile"]));
            }
            set {
                this["GPXFile"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool UseLuckyEggs {
            get {
                return ((bool)(this["UseLuckyEggs"]));
            }
            set {
                this["UseLuckyEggs"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool NotTransferPokemonsThatCanEvolve {
            get {
                return ((bool)(this["NotTransferPokemonsThatCanEvolve"]));
            }
            set {
                this["NotTransferPokemonsThatCanEvolve"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool EvolveOnlyPokemonAboveIV {
            get {
                return ((bool)(this["EvolveOnlyPokemonAboveIV"]));
            }
            set {
                this["EvolveOnlyPokemonAboveIV"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool EvolvePokemon {
            get {
                return ((bool)(this["EvolvePokemon"]));
            }
            set {
                this["EvolvePokemon"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("95")]
        public float EvolveOnlyPokemonAboveIVValue {
            get {
                return ((float)(this["EvolveOnlyPokemonAboveIVValue"]));
            }
            set {
                this["EvolveOnlyPokemonAboveIVValue"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool TransferPokemon {
            get {
                return ((bool)(this["TransferPokemon"]));
            }
            set {
                this["TransferPokemon"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool UseTransferPokemonKeepAllAboveCP {
            get {
                return ((bool)(this["UseTransferPokemonKeepAllAboveCP"]));
            }
            set {
                this["UseTransferPokemonKeepAllAboveCP"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool UseTransferPokemonKeepAllAboveIV {
            get {
                return ((bool)(this["UseTransferPokemonKeepAllAboveIV"]));
            }
            set {
                this["UseTransferPokemonKeepAllAboveIV"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool UsePokemonToNotTransferList {
            get {
                return ((bool)(this["UsePokemonToNotTransferList"]));
            }
            set {
                this["UsePokemonToNotTransferList"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("GoogleEmail")]
        public string GoogleEmail {
            get {
                return ((string)(this["GoogleEmail"]));
            }
            set {
                this["GoogleEmail"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("GooglePassword")]
        public string GooglePassword {
            get {
                return ((string)(this["GooglePassword"]));
            }
            set {
                this["GooglePassword"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool CatchPokemon {
            get {
                return ((bool)(this["CatchPokemon"]));
            }
            set {
                this["CatchPokemon"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool GPXIgnorePokestops {
            get {
                return ((bool)(this["GPXIgnorePokestops"]));
            }
            set {
                this["GPXIgnorePokestops"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool DebugMode {
            get {
                return ((bool)(this["DebugMode"]));
            }
            set {
                this["DebugMode"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("200")]
        public int EvolveKeepCandiesValue {
            get {
                return ((int)(this["EvolveKeepCandiesValue"]));
            }
            set {
                this["EvolveKeepCandiesValue"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool UseTeleportInsteadOfWalking {
            get {
                return ((bool)(this["UseTeleportInsteadOfWalking"]));
            }
            set {
                this["UseTeleportInsteadOfWalking"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool UsePokemonToEvolveList {
            get {
                return ((bool)(this["UsePokemonToEvolveList"]));
            }
            set {
                this["UsePokemonToEvolveList"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool CatchIncensePokemon {
            get {
                return ((bool)(this["CatchIncensePokemon"]));
            }
            set {
                this["CatchIncensePokemon"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1")]
        public int TransferPokemonKeepAmountHighestCP {
            get {
                return ((int)(this["TransferPokemonKeepAmountHighestCP"]));
            }
            set {
                this["TransferPokemonKeepAmountHighestCP"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1")]
        public int TransferPokemonKeepAmountHighestIV {
            get {
                return ((int)(this["TransferPokemonKeepAmountHighestIV"]));
            }
            set {
                this["TransferPokemonKeepAmountHighestIV"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool CatchLuredPokemon {
            get {
                return ((bool)(this["CatchLuredPokemon"]));
            }
            set {
                this["CatchLuredPokemon"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("15")]
        public int ExportPokemonToCsvEveryMinutes {
            get {
                return ((int)(this["ExportPokemonToCsvEveryMinutes"]));
            }
            set {
                this["ExportPokemonToCsvEveryMinutes"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool HatchEggs {
            get {
                return ((bool)(this["HatchEggs"]));
            }
            set {
                this["HatchEggs"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool UseOnlyBasicIncubator {
            get {
                return ((bool)(this["UseOnlyBasicIncubator"]));
            }
            set {
                this["UseOnlyBasicIncubator"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("random")]
        public string DevicePackageName {
            get {
                return ((string)(this["DevicePackageName"]));
            }
            set {
                this["DevicePackageName"] = value;
            }
        }
    }
}
