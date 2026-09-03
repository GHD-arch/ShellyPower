using System.Reflection;
using System.Runtime.InteropServices;
// GUID stable utilisé pour persister les options du plugin via NINA.Profile.PluginOptionsAccessor
// et lu par PluginBase.Identifier.
[assembly: Guid("8F6E4B2A-1C3D-4E5F-9A0B-C1D2E3F4A5B6")]

// ---------------------------------------------------------------------------
// Propriétés du manifeste lues DIRECTEMENT depuis des attributs d'assembly
// (vérifié par réflexion IL sur NINA.Plugin.PluginBase) :
//   Name                    -> AssemblyTitleAttribute.Title
//   Author                  -> AssemblyCompanyAttribute.Company
//   Version                 -> AssemblyFileVersionAttribute.Version
//   Identifier              -> GuidAttribute.Value
//   Descriptions.Short      -> AssemblyDescriptionAttribute.Description
//   Tags, License, URLs, MinimumApplicationVersion, LongDescription
//                           -> AssemblyMetadataAttribute (clés ci-dessous)
// ---------------------------------------------------------------------------
[assembly: AssemblyTitle("Shelly Power")]
[assembly: AssemblyCompany("Gérard Hurtaud")]
[assembly: AssemblyDescription("Control Shelly smart plugs (4 named plugs) from NINA.")]
[assembly: AssemblyFileVersion("1.4.0.0")]

[assembly: AssemblyMetadata("License", "MIT")]
[assembly: AssemblyMetadata("LicenseURL", "https://opensource.org/licenses/MIT")]
[assembly: AssemblyMetadata("Homepage", "https://github.com/GHD-arch/ShellyPower")]
[assembly: AssemblyMetadata("Repository", "https://github.com/GHD-arch/ShellyPower")]
[assembly: AssemblyMetadata("Tags", "Shelly,Power,Switch")]
[assembly: AssemblyMetadata("MinimumApplicationVersion", "3.0.0.0")]
[assembly: AssemblyMetadata("LongDescription", "Control Shelly smart plugs (4 named plugs) from NINA: switch equipment, sequencer ON/OFF instructions and a configuration panel.")]