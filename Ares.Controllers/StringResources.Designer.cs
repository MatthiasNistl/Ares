﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Ares.Controllers {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class StringResources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal StringResources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Ares.Controllers.StringResources", typeof(StringResources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Schließe Verbindung mit Server.
        /// </summary>
        internal static string ClosingConnection {
            get {
                return ResourceManager.GetString("ClosingConnection", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Verbunden mit {0}.
        /// </summary>
        internal static string ConnectedWith {
            get {
                return ResourceManager.GetString("ConnectedWith", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Timeout bei der Verbindung zum Server.
        /// </summary>
        internal static string ConnectTimeout {
            get {
                return ResourceManager.GetString("ConnectTimeout", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Keine Verbindung zum Server.
        /// </summary>
        internal static string NoConnection {
            get {
                return ResourceManager.GetString("NoConnection", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Kein Ping empfangen, nehme Netzwerkfehler an.
        /// </summary>
        internal static string NoPingFailure {
            get {
                return ResourceManager.GetString("NoPingFailure", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Player hat falsche Version, beende Verbindung.
        /// </summary>
        internal static string PlayerHasWrongVersion {
            get {
                return ResourceManager.GetString("PlayerHasWrongVersion", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Sende Bytes: [{0} {1} {2}].
        /// </summary>
        internal static string SendingBytes {
            get {
                return ResourceManager.GetString("SendingBytes", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Sende Client-Info: .
        /// </summary>
        internal static string SendingControlInfo {
            get {
                return ResourceManager.GetString("SendingControlInfo", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Sende Taste: .
        /// </summary>
        internal static string SendingKey {
            get {
                return ResourceManager.GetString("SendingKey", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Sende Ping.
        /// </summary>
        internal static string SendingPing {
            get {
                return ResourceManager.GetString("SendingPing", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Starte Serversuche.
        /// </summary>
        internal static string StartServerSearch {
            get {
                return ResourceManager.GetString("StartServerSearch", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Stoppe Serversuche.
        /// </summary>
        internal static string StopServerSearch {
            get {
                return ResourceManager.GetString("StopServerSearch", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to UDP-Paket empfangen: .
        /// </summary>
        internal static string UDPReceived {
            get {
                return ResourceManager.GetString("UDPReceived", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Taste {0} wird nicht unterstützt.
        /// </summary>
        internal static string UnsupportedKey {
            get {
                return ResourceManager.GetString("UnsupportedKey", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Ignoriere Player mit falscher Kennung / falscher Version.
        /// </summary>
        internal static string WrongPlayerIgnored {
            get {
                return ResourceManager.GetString("WrongPlayerIgnored", resourceCulture);
            }
        }
    }
}
