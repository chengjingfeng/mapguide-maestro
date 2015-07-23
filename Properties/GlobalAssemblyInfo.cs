#region Disclaimer / License

// Copyright (C) 2010, Jackie Ng
// http://trac.osgeo.org/mapguide/wiki/maestro, jumpinjackie@gmail.com
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
//

#endregion Disclaimer / License

using System.Reflection;

[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Open Source Geospatial Foundation")]
[assembly: AssemblyCopyright("Copyright (c) 2015, Jackie Ng")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("6.0.0.0")]
[assembly: AssemblyFileVersion("6.0.0.0")]

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Redundancies in Code", "RECS0145:Removes 'private' modifiers that are not required", Justification = "The author likes to be explicit with accessibility modifiers")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Redundancies in Code", "RECS0129:Removes 'internal' modifiers that are not required", Justification = "The author likes to be explicit with accessibility modifiers")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Language", "CSE0003:Use expression-bodied members", Justification = "The author prefers debuggability over succintness")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Redundancies in Symbol Declarations", "RECS0007:The default underlying type of enums is int, so defining it explicitly is redundant.", Justification = "The author prefers to be explicit about underlying enum types")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Redundancies in Symbol Declarations", "RECS0072", Justification = "The author prefers explicit context for non-field member access")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Redundancies in Symbol Declarations", "IDE0003", Justification = "The author prefers explicit context for non-field member access")]