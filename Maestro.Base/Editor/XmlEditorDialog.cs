﻿#region Disclaimer / License
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
#endregion
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Maestro.Editors.Generic;
using System.IO;
using OSGeo.MapGuide.MaestroAPI;
using System.Xml;
using Maestro.Editors;
using System.Xml.Schema;
using Maestro.Base.UI.Preferences;
using ICSharpCode.Core;

namespace Maestro.Base.Editor
{
    public partial class XmlEditorDialog : Form, INotifyResourceChanged
    {
        private XmlEditorCtrl _ed;
        private IEditorService _edSvc;

        internal XmlEditorDialog()
        {
            InitializeComponent();
            _ed = new XmlEditorCtrl();
            _ed.Validator = new XmlValidationCallback(ValidateXml);
            _ed.Dock = DockStyle.Fill;
            contentPanel.Controls.Add(_ed);
            this.XsdPath = PropertyService.Get(ConfigProperties.XsdSchemaPath, ConfigProperties.DefaultXsdSchemaPath);
        }

        public string XsdPath
        {
            get;
            set;
        }

        public XmlEditorDialog(IEditorService edsvc)
            : this()
        {
            _edSvc = edsvc;
            _edSvc.RegisterCustomNotifier(this);
            this.Disposed += new EventHandler(OnDisposed);
        }

        void OnDisposed(object sender, EventArgs e)
        {
            //Same as EditorBindableCollapsiblePanel.UnsubscribeEventHandlers()
            var handler = this.ResourceChanged;
            if (handler != null)
            {
                foreach (var h in handler.GetInvocationList())
                {
                    this.ResourceChanged -= (EventHandler)h;
                }
                //In case we left out something (shouldn't be)
                this.ResourceChanged = null;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            if (_edSvc != null)
                _ed.InitResourceData(_edSvc);
        }

        public ResourceTypes ResourceType
        {
            get;
            private set;
        }

        private bool _enableResourceTypeValidation = false;

        public void SetXmlContent(string xml, ResourceTypes type)
        {
            _ed.XmlContent = xml;
            this.ResourceType = type;
            _enableResourceTypeValidation = true;
        }

        private string _lastSnapshot;

        /// <summary>
        /// Gets or sets the XML content for this dialog.
        /// </summary>
        public string XmlContent
        {
            get { return _ed.XmlContent; }
            set { _ed.XmlContent = _lastSnapshot = value; }
        }

        private XmlSchema GetXsd(string xsdFile)
        {
            string path = xsdFile;

            if (!string.IsNullOrEmpty(this.XsdPath))
                path = Path.Combine(this.XsdPath, xsdFile);

            if (File.Exists(path))
            {
                ValidationEventHandler handler = (s, e) =>
                {
                };
                return XmlSchema.Read(File.OpenRead(path), handler);
            }
            return null;
        }

        private void ValidateXml(out string[] errors, out string[] warnings)
        {
            errors = new string[0];
            warnings = new string[0];

            List<string> err = new List<string>();
            List<string> warn = new List<string>();

            var res = _edSvc.GetEditedResource();

            //Test for well-formedness
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(_ed.XmlContent);
            }
            catch (XmlException ex)
            {
                err.Add(ex.Message);
            }

            //If strongly-typed, test that this is serializable
            if (res.IsStronglyTyped)
            {
                try
                {
                    //Test by simply attempting to deserialize the current xml content
                    using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(_ed.XmlContent)))
                    {
                        //Use original resource type to determine how to deserialize
                        var obj = ResourceTypeRegistry.Deserialize(res.ResourceType, ms);
                    }
                }
                catch (Exception ex)
                {
                    err.Add(ex.Message);
                }
            }

            //Finally verify the content itself
            var xml = this.XmlContent;
            var xsd = GetXsd(res.ValidatingSchema);
            var validator = new XmlValidator();
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
            {
                validator.Validate(ms, xsd);
            }

            err.AddRange(validator.ValidationErrors);
            warn.AddRange(validator.ValidationWarnings);

            /*
            var xml = this.XmlContent;
            var config = new XmlReaderSettings();
            
            config.ValidationType = ValidationType.Schema;
            config.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
            config.ValidationFlags |= XmlSchemaValidationFlags.ProcessInlineSchema;
            config.ValidationFlags |= XmlSchemaValidationFlags.ProcessSchemaLocation;
            //This will trap all the errors and warnings that are raised
            config.ValidationEventHandler += (s, e) =>
            {
                if (e.Severity == XmlSeverityType.Warning)
                {
                    warn.Add(e.Message);
                }
                else
                {
                    err.Add(e.Message);
                }
            };

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
            {
                using (var reader = XmlReader.Create(ms, config))
                {
                    while (reader.Read()) { } //Trigger the validation
                }
            }*/

            errors = err.ToArray();
            warnings = warn.ToArray();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (_ed.PerformValidation(true, true))
            {
                if (_lastSnapshot != _ed.XmlContent)
                    OnResourceChanged();
                this.DialogResult = DialogResult.OK;
            }
        }

        private void OnResourceChanged()
        {
            var handler = this.ResourceChanged;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        public event EventHandler ResourceChanged;
    }
}