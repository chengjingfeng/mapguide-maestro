﻿#region Disclaimer / License

// Copyright (C) 2010, Jackie Ng
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

using Maestro.Shared.UI;
using OSGeo.MapGuide.MaestroAPI;
using OSGeo.MapGuide.ObjectModels;
using System;
using System.IO;

namespace Maestro.Base.Services.DragDropHandlers
{
    internal class SdfFileHandler : IDragDropHandler
    {
        public string HandlerAction
        {
            get { return Strings.SdfHandlerAction; }
        }

        private readonly string[] extensions = { ".sdf" }; //NOXLATE

        public bool HandleDrop(IServerConnection conn, string file, string folderId)
        {
            try
            {
                var wb = Workbench.Instance;
                var exp = wb.ActiveSiteExplorer;
                var fs = ObjectFactory.CreateFeatureSource("OSGeo.SDF"); //NOXLATE

                string fileName = Path.GetFileName(file);
                string resName = Path.GetFileNameWithoutExtension(file);
                int counter = 0;
                string resId = $"{folderId + resName}.FeatureSource"; //NOXLATE
                while (conn.ResourceService.ResourceExists(resId))
                {
                    counter++;
                    resId = $"{folderId + resName} ({counter}).FeatureSource"; //NOXLATE
                }
                fs.ResourceID = resId;
                fs.SetConnectionProperty("File", StringConstants.MgDataFilePath + fileName); //NOXLATE
                conn.ResourceService.SaveResource(fs);

                using (var stream = File.Open(file, FileMode.Open))
                {
                    conn.ResourceService.SetResourceData(fs.ResourceID, fileName, OSGeo.MapGuide.ObjectModels.Common.ResourceDataType.File, stream);
                }

                return true;
            }
            catch (Exception ex)
            {
                ErrorDialog.Show(ex);
                return false;
            }
        }

        public override string ToString()
        {
            return Strings.SdfHandlerAction;
        }

        public bool CanHandleFileExtension(string fileExtension)
        {
            return HandlerUtil.ExtensionInList(extensions, fileExtension);
        }
    }
}