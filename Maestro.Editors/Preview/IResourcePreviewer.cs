﻿#region Disclaimer / License

// Copyright (C) 2013, Jackie Ng
// https://github.com/jumpinjackie/mapguide-maestro
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

using OSGeo.MapGuide.MaestroAPI;
using OSGeo.MapGuide.ObjectModels;

namespace Maestro.Editors.Preview
{
    /// <summary>
    /// Defines an interface for previewing resources
    /// </summary>
    public interface IResourcePreviewer
    {
        /// <summary>
        /// Gets whether the specified resource can be previewed
        /// </summary>
        /// <param name="res"></param>
        /// <param name="conn"></param>
        /// <returns></returns>
        bool IsPreviewable(IResource res, IServerConnection conn);

        /// <summary>
        /// Previews the specified resource
        /// </summary>
        /// <param name="res">The resource to be previewed</param>
        /// <param name="edSvc">The editor service</param>
        void Preview(IResource res, IEditorService edSvc);

        /// <summary>
        /// Previews the specified resource using the given locale
        /// </summary>
        /// <param name="res">The resource to be previewed</param>
        /// <param name="edSvc">The editor service</param>
        /// <param name="locale">The locale</param>
        /// <remarks>
        /// The locale parameter should be treated as a hint. The underlying <see cref="T:OSGeo.MapGuide.MaestroAPI.IServerConnection"/> implementation
        /// may not actually respect this value.
        /// </remarks>
        void Preview(IResource res, IEditorService edSvc, string locale);
    }
}