﻿#region Disclaimer / License

// Copyright (C) 2017, Jackie Ng
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

using OSGeo.MapGuide.MaestroAPI.Services;
using System.IO;
using System.Net;

namespace OSGeo.MapGuide.MaestroAPI.Tile
{
    /// <summary>
    /// An implementation of <see cref="OSGeo.MapGuide.MaestroAPI.Services.ITileService"/> for fetching XYZ tiles
    /// </summary>
    public class XYZTileService : ITileService
    {
        readonly string _urlTemplate;

        /// <summary>
        /// Constructs a new instance
        /// </summary>
        /// <param name="urlTemplate">The URL of the XYZ tile service/endpoint. The URL must have {x}, {y} and {z} placeholders</param>
        public XYZTileService(string urlTemplate)
        {
            //Convert into a string.Format-able form
            _urlTemplate = urlTemplate.Replace("{x}", "{0}").Replace("{y}", "{1}").Replace("{z}", "{2}");
        }

        /// <summary>
        /// Gets the tile at the given XYZ coordinate
        /// </summary>
        /// <param name="mapDefinition">Un-used parameter</param>
        /// <param name="baseLayerGroup">Un-used parameters</param>
        /// <param name="column">Y value</param>
        /// <param name="row">X value</param>
        /// <param name="scaleIndex">Z value</param>
        /// <returns></returns>
        public Stream GetTile(string mapDefinition, string baseLayerGroup, int column /* Y */, int row /* X */, int scaleIndex /* Z */)
        {
            var url = string.Format(_urlTemplate, row, column, scaleIndex);
            var req = HttpWebRequest.Create(url);

            var resp = req.GetResponse();
            return resp.GetResponseStream();
        }
    }
}
