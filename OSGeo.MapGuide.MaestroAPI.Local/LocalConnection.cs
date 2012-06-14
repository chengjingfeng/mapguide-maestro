﻿#region Disclaimer / License
// Copyright (C) 2011, Jackie Ng
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
using System.Text;
using OSGeo.MapGuide.MaestroAPI.Services;
using OSGeo.MapGuide.MaestroAPI.Native;
using System.Collections.Specialized;
using OSGeo.MapGuide.ObjectModels.Common;
using OSGeo.MapGuide.MaestroAPI.Resource;
using OSGeo.MapGuide.MaestroAPI.Exceptions;
using OSGeo.MapGuide.ObjectModels.Capabilities;
using OSGeo.MapGuide.MaestroAPI.Feature;
using OSGeo.MapGuide.MaestroAPI.Schema;
using System.IO;
using OSGeo.MapGuide.MaestroAPI.CoordinateSystem;
using System.Diagnostics;
using OSGeo.MapGuide.MaestroAPI.Local.Commands;
using OSGeo.MapGuide.MaestroAPI.Commands;

namespace OSGeo.MapGuide.MaestroAPI.Local
{
    public class LocalConnection : PlatformConnectionBase,
                                   IServerConnection,
                                   IFeatureService,
                                   IResourceService,
                                   ITileService,
                                   //IMappingService,
                                   IDrawingService
    {
        public event EventHandler SessionIDChanged; //Not used

        public static LocalConnection Create(NameValueCollection initParams)
        {
            return new LocalConnection(initParams);
        }

        static bool _init = false;

        private MgServiceFactory _fact;

        protected LocalConnection(NameValueCollection initParams) : base()
        {
            _fact = new MgServiceFactory();
            _sessionId = Guid.NewGuid().ToString();
            _configFile = initParams[PARAM_CONFIG];

            var sw = new Stopwatch();
            sw.Start();
            MgPlatform.Initialize(_configFile);
            _init = true;
            sw.Stop();
            Trace.TraceInformation("MapGuide Platform initialized in {0}ms", sw.ElapsedMilliseconds);
        }

        public override OSGeo.MapGuide.MaestroAPI.Commands.ICommand CreateCommand(int cmdType)
        {
            CommandType ct = (CommandType)cmdType;
            switch (ct)
            {
                case CommandType.ApplySchema:
                    return new LocalNativeApplySchema(this);
                case CommandType.CreateDataStore:
                    return new LocalNativeCreateDataStore(this);
                case CommandType.DeleteFeatures:
                    return new LocalNativeDelete(this);
                case CommandType.InsertFeature:
                    return new LocalNativeInsert(this);
                case CommandType.UpdateFeatures:
                    return new LocalNativeUpdate(this);
            }
            return base.CreateCommand(cmdType);
        }

        public const string PROVIDER_NAME = "Maestro.Local";

        public override string ProviderName
        {
            get { return PROVIDER_NAME; }
        }

        public static ConnectionProviderEntry ProviderInfo
        {
            get
            {
                return new ConnectionProviderEntry(
                    PROVIDER_NAME,
                    "Connection using the MapGuide Desktop API", //LOCALIZEME
                    false);
            }
        }

        public override System.Collections.Specialized.NameValueCollection CloneParameters
        {
            get 
            {
                return new NameValueCollection()
                {
                    { PARAM_CONFIG, _configFile }
                };
            }
        }

        private string _sessionId;

        public override string SessionID
        {
            get
            {
                return _sessionId;
            }
        }

        protected override IServerConnection GetInterface()
        {
            return this;
        }

        private string _configFile;
        const string PARAM_CONFIG = "ConfigFile";

        private MgdResourceService _resSvc;
        private MgdFeatureService _featSvc;
        private MgDrawingService _drawSvc;
        private MgRenderingService _renderSvc;
        private MgTileService _tileSvc;

        public override void Dispose()
        {
            if (_resSvc != null)
            {
                _resSvc.Dispose();
                _resSvc = null;
            }

            if (_featSvc != null)
            {
                _featSvc.Dispose();
                _featSvc = null;
            }

            if (_drawSvc != null)
            {
                _drawSvc.Dispose();
                _drawSvc = null;
            }

            if (_renderSvc != null)
            {
                _renderSvc.Dispose();
                _renderSvc = null;
            }

            if (_tileSvc != null)
            {
                _tileSvc.Dispose();
                _tileSvc = null;
            }
        }

        private MgdResourceService GetResourceService()
        {
            if (_resSvc == null)
                _resSvc = (MgdResourceService)_fact.CreateService(MgServiceType.ResourceService);

            return _resSvc;
        }

        private MgdFeatureService GetFeatureService()
        {
            if (_featSvc == null)
                _featSvc = (MgdFeatureService)_fact.CreateService(MgServiceType.FeatureService);

            return _featSvc;
        }

        private MgDrawingService GetDrawingService()
        {
            if (_drawSvc == null)
                _drawSvc = (MgDrawingService)_fact.CreateService(MgServiceType.DrawingService);

            return _drawSvc;
        }

        private MgRenderingService GetRenderingService()
        {
            if (_renderSvc == null)
                _renderSvc = (MgRenderingService)_fact.CreateService(MgServiceType.RenderingService);

            return _renderSvc;
        }

        private MgTileService GetTileService()
        {
            if (_tileSvc == null)
                _tileSvc = (MgTileService)_fact.CreateService(MgServiceType.TileService);

            return _tileSvc;
        }

        public override IServerConnection Clone()
        {
            return LocalConnection.Create(this.CloneParameters);
        }

        private void LogMethodCall(string method, bool success, params string[] values)
        {
            OnRequestDispatched(method + "(" + string.Join(", ", values) + ") " + ((success) ? "Success" : "Failure"));
        }

        public override System.IO.Stream GetResourceXmlData(string resourceID)
        {
            var res = GetResourceService();
            //var result = Native.Utility.MgStreamToNetStream(res, res.GetType().GetMethod("GetResourceContent"), new object[] { new MgResourceIdentifier(resourceID) });
            GetByteReaderMethod fetch = () =>
            {
                MgResourceIdentifier resId = new MgResourceIdentifier(resourceID);
                return res.GetResourceContent(resId);
            };
            LogMethodCall("MgResourceService::GetResourceContent", true, resourceID);
            return new MgReadOnlyStream(fetch);
        }

        public override void DeleteResource(string resourceID)
        {
            var res = GetResourceService();
            res.DeleteResource(new MgResourceIdentifier(resourceID));
            LogMethodCall("MgResourceService::DeleteResource", true, resourceID);
            OnResourceDeleted(resourceID);
        }

        public override ResourceList GetRepositoryResources(string startingpoint, string type, int depth, bool computeChildren)
        {
            if (type == null)
                type = "";
            var res = GetResourceService();
            GetByteReaderMethod fetch = () => 
            {
                MgResourceIdentifier resId = new MgResourceIdentifier(startingpoint);
                return res.EnumerateResources(resId, depth, type, computeChildren);
            };
            LogMethodCall("MgResourceService::EnumerateResources", true, startingpoint, depth.ToString(), type, computeChildren.ToString());
            return base.DeserializeObject<ResourceList>(new MgReadOnlyStream(fetch));
        }

        public override ResourceReferenceList EnumerateResourceReferences(string resourceid)
        {
            return new ResourceReferenceList()
            {
                ResourceId = new System.ComponentModel.BindingList<string>()
            };
        }

        public override void CopyResource(string oldpath, string newpath, bool overwrite)
        {
            bool exists = ResourceExists(newpath);

            var res = GetResourceService();
            res.CopyResource(new MgResourceIdentifier(oldpath), new MgResourceIdentifier(newpath), overwrite);
            LogMethodCall("MgResourceService::CopyResource", true, oldpath, newpath, overwrite.ToString());
            if (exists)
                OnResourceUpdated(newpath);
            else
                OnResourceAdded(newpath);
        }

        public override void CopyFolder(string oldpath, string newpath, bool overwrite)
        {
            bool exists = ResourceExists(newpath);
            if (!oldpath.EndsWith("/"))
                oldpath += "/";
            if (!newpath.EndsWith("/"))
                newpath += "/";

            var res = GetResourceService();
            res.CopyResource(new MgResourceIdentifier(oldpath), new MgResourceIdentifier(newpath), overwrite);
            LogMethodCall("MgResourceService::CopyResource", true, oldpath, newpath, overwrite.ToString());
            if (exists)
                OnResourceUpdated(newpath);
            else
                OnResourceAdded(newpath);
        }

        public override void MoveResource(string oldpath, string newpath, bool overwrite)
        {
            bool exists = ResourceExists(newpath);

            var res = GetResourceService();
            res.MoveResource(new MgResourceIdentifier(oldpath), new MgResourceIdentifier(newpath), overwrite);
            LogMethodCall("MgResourceService::MoveResource", true, oldpath, newpath, overwrite.ToString());
            OnResourceDeleted(oldpath);
            if (exists)
                OnResourceUpdated(newpath);
            else
                OnResourceAdded(newpath);
        }

        public override void MoveFolder(string oldpath, string newpath, bool overwrite)
        {
            bool exists = ResourceExists(newpath);
            if (!oldpath.EndsWith("/"))
                oldpath += "/";
            if (!newpath.EndsWith("/"))
                newpath += "/";

            var res = GetResourceService();
            res.MoveResource(new MgResourceIdentifier(oldpath), new MgResourceIdentifier(newpath), overwrite);
            LogMethodCall("MgResourceService::MoveResource", true, oldpath, newpath, overwrite.ToString());
            OnResourceDeleted(oldpath);
            if (exists)
                OnResourceUpdated(newpath);
            else
                OnResourceAdded(newpath);
        }

        public override System.IO.Stream GetResourceData(string resourceID, string dataname)
        {
            var res = GetResourceService();
            //var result = Native.Utility.MgStreamToNetStream(res, res.GetType().GetMethod("GetResourceData"), new object[] { new MgResourceIdentifier(resourceID), dataname });
            GetByteReaderMethod fetch = () => 
            {
                MgResourceIdentifier resId = new MgResourceIdentifier(resourceID);
                return res.GetResourceData(resId, dataname);
            };
            LogMethodCall("MgResourceService::GetResourceData", true, resourceID, dataname);
            return new MgReadOnlyStream(fetch);
        }

        public override void SetResourceData(string resourceid, string dataname, ResourceDataType datatype, System.IO.Stream stream, Utility.StreamCopyProgressDelegate callback)
        {
            byte[] data = Utility.StreamAsArray(stream);
            if (callback != null)
                callback(0, data.Length, data.Length);
            var res = GetResourceService();
            MgByteSource source = new MgByteSource(data, data.Length);
            MgByteReader reader = source.GetReader();
            res.SetResourceData(new MgResourceIdentifier(resourceid), dataname, datatype.ToString(), reader);
            LogMethodCall("MgResourceService::SetResourceData", true, resourceid, dataname, datatype.ToString(), "MgByteReader");
            if (callback != null)
                callback(data.Length, 0, data.Length);
        }

        public override void UploadPackage(string filename, Utility.StreamCopyProgressDelegate callback)
        {
            System.IO.FileInfo fi = new System.IO.FileInfo(filename);

            if (callback != null)
                callback(0, fi.Length, fi.Length);

            var res = GetResourceService();
            MgByteSource pkgSource = new MgByteSource(filename);
            MgByteReader rd = pkgSource.GetReader();
            res.ApplyResourcePackage(rd);
            rd.Dispose();
            LogMethodCall("MgResourceService::ApplyResourcePackage", true, "MgByteReader");
            if (callback != null)
                callback(fi.Length, 0, fi.Length);
        }

        public override void UpdateRepository(string resourceId, OSGeo.MapGuide.ObjectModels.Common.ResourceFolderHeaderType header)
        {
            throw new NotImplementedException();
        }

        public override object GetFolderOrResourceHeader(string resourceId)
        {
            if (ResourceIdentifier.IsFolderResource(resourceId))
            {
                return ResourceFolderHeaderType.CreateDefault();
            }
            else
            {
                return ResourceDocumentHeaderType.CreateDefault();
            }
        }

        public override void SetResourceXmlData(string resourceid, System.IO.Stream content, System.IO.Stream header)
        {
            bool exists = ResourceExists(resourceid);

            var res = GetResourceService();
            byte[] bufHeader = header == null ? new byte[0] : Utility.StreamAsArray(header);
            byte[] bufContent = content == null ? new byte[0] : Utility.StreamAsArray(content);
            //MgByteReader rH = bufHeader.Length == 0 ? null : new MgByteReader(bufHeader, bufHeader.Length, "text/xml");
            //MgByteReader rC = bufContent.Length == 0 ? null : new MgByteReader(bufContent, bufContent.Length, "text/xml");
            MgByteReader rH = null;
            MgByteReader rC = null;

            if (bufHeader.Length > 0)
            {
                MgByteSource source = new MgByteSource(bufHeader, bufHeader.Length);
                source.SetMimeType("text/xml");
                rH = source.GetReader();
            }

            if (bufContent.Length > 0)
            {
                MgByteSource source = new MgByteSource(bufContent, bufContent.Length);
                source.SetMimeType("text/xml");
                rC = source.GetReader();
            }

            res.SetResource(new MgResourceIdentifier(resourceid), rC, rH);
            LogMethodCall("MgResourceService::SetResource", true, resourceid, "MgByteReader", "MgByteReader");
            if (exists)
                OnResourceUpdated(resourceid);
            else
                OnResourceAdded(resourceid);
        }

        public override UnmanagedDataList EnumerateUnmanagedData(string startpath, string filter, bool recursive, UnmanagedDataTypes type)
        {
            var resSvc = GetResourceService();
            var br = resSvc.EnumerateUnmanagedData(startpath, recursive, type.ToString(), filter);
            var result = (UnmanagedDataList)base.DeserializeObject(typeof(UnmanagedDataList), new MgReadOnlyStream(new GetByteReaderMethod(() => { return br; })));
            LogMethodCall("MgResourceService::EnumerateUnmanagedData", true, startpath, recursive.ToString(), type.ToString(), filter);
            return result;
        }

        public override string TestConnection(string featuresource)
        {
            var fes = GetFeatureService();
            var res = fes.TestConnection(new MgResourceIdentifier(featuresource)) ? "True" : "Unspecified errors";
            LogMethodCall("MgFeatureService::TestConnection", true, featuresource);
            return res;
        }

        public override FeatureProviderRegistryFeatureProvider[] FeatureProviders
        {
            get 
            {
                MgFeatureService fes = GetFeatureService();
                GetByteReaderMethod fetch = () =>
                {
                    return fes.GetFeatureProviders();
                };
                LogMethodCall("MgFeatureService::GetFeatureProviders", true);
                var reg = base.DeserializeObject<FeatureProviderRegistry>(new MgReadOnlyStream(fetch));
                return reg.FeatureProvider.ToArray();
            }
        }

        public override FdoSpatialContextList GetSpatialContextInfo(string resourceID, bool activeOnly)
        {
            var fes = GetFeatureService();
            MgSpatialContextReader rd = fes.GetSpatialContexts(new MgResourceIdentifier(resourceID), activeOnly);
            GetByteReaderMethod fetch = () =>
            {
                return rd.ToXml();
            };
            LogMethodCall("MgFeatureService::GetSpatialContexts", true, resourceID, activeOnly.ToString());
            return base.DeserializeObject<FdoSpatialContextList>(new MgReadOnlyStream(fetch));
        }

        public override string[] GetIdentityProperties(string resourceID, string classname)
        {
            var fes = GetFeatureService();
            string[] parts = classname.Split(':');
            MgResourceIdentifier resId = new MgResourceIdentifier(resourceID);
            MgPropertyDefinitionCollection props;
            if (parts.Length == 1)
                parts = new string[] { classname };
            else if (parts.Length != 2)
                throw new Exception("Unable to parse classname into class and schema: " + classname);

            var schemas = fes.DescribeSchema(resId, parts[0]);

            var classes = schemas.GetItem(0).GetClasses();

            LogMethodCall("MgFeatureService::DescribeSchema", true, resourceID, parts[0]);

            int ccount = classes.GetCount();
            for (int i = 0; i < ccount; i++)
            {
                MgClassDefinition cdef = classes.GetItem(i);
                if (parts.Length == 1 || cdef.Name.ToLower().Trim().Equals(parts[1].ToLower().Trim()))
                {
                    props = cdef.GetIdentityProperties();

                    int pcount = props.GetCount();
                    string[] res = new string[pcount];
                    for (int j = 0; j < pcount; j++)
                        res[j] = (props.GetItem(j) as MgProperty).Name;

                    return res;
                }
            }

            throw new Exception("Unable to find class: " + parts[1] + " in schema " + parts[0]);
        }

        protected override FeatureSourceDescription DescribeFeatureSourceInternal(string resourceID)
        {
            var fes = GetFeatureService();
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(fes.DescribeSchemaAsXml(new MgResourceIdentifier(resourceID), "")));

            LogMethodCall("MgFeatureService::DescribeSchemaAsXml", true, resourceID, "");

            return new FeatureSourceDescription(ms);
        }

        public override FeatureSchema DescribeFeatureSourcePartial(string resourceID, string schema, string[] classNames)
        {
            var fes = GetFeatureService();
            MgStringCollection names = new MgStringCollection();
            foreach (var clsName in classNames)
            {
                names.Add(clsName);
            }
            string xml = fes.DescribeSchemaAsXml(new MgResourceIdentifier(resourceID), schema, names);
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            LogMethodCall("MgFeatureService::DescribeSchemaAsXml", true, resourceID, schema, "{" + string.Join(",", classNames) + "}");
            return new FeatureSourceDescription(ms).Schemas[0];
        }

        public override FeatureSchema DescribeFeatureSource(string resourceID, string schema)
        {
            var fes = GetFeatureService();
            if (schema != null && schema.IndexOf(":") > 0)
                schema = schema.Split(':')[0];
            System.IO.MemoryStream ms = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(fes.DescribeSchemaAsXml(new MgResourceIdentifier(resourceID), schema)));

            LogMethodCall("MgFeatureService::DescribeSchemaAsXml", true, resourceID, schema);

            return new FeatureSourceDescription(ms).Schemas[0];
        }

        public override IReader AggregateQueryFeatureSource(string resourceID, string schema, string filter, string[] columns)
        {
            return AggregateQueryFeatureSourceCore(resourceID, schema, filter, columns, null);
        }

        public override IReader AggregateQueryFeatureSource(string resourceID, string schema, string filter, System.Collections.Specialized.NameValueCollection aggregateFunctions)
        {
            return AggregateQueryFeatureSourceCore(resourceID, schema, filter, null, aggregateFunctions);
        }

        private IReader AggregateQueryFeatureSourceCore(string resourceID, string schema, string query, string[] columns, System.Collections.Specialized.NameValueCollection computedProperties)
        {
            var fes = GetFeatureService();
            MgFeatureAggregateOptions mgf = new MgFeatureAggregateOptions();
            if (query != null)
                mgf.SetFilter(query);

            if (columns != null && columns.Length != 0)
                foreach (string s in columns)
                    mgf.AddFeatureProperty(s);

            if (computedProperties != null && computedProperties.Count > 0)
                foreach (string s in computedProperties.Keys)
                    mgf.AddComputedProperty(s, computedProperties[s]);

            var reader = fes.SelectAggregate(new MgResourceIdentifier(resourceID), schema, mgf);

            LogMethodCall("MgFeatureService::SelectAggregate", true, resourceID, schema, "MgFeatureAggregateOptions");

            return new LocalNativeDataReader(reader);
        }

        public override DataStoreList EnumerateDataStores(string providerName, string partialConnString)
        {
            var fes = GetFeatureService();
            GetByteReaderMethod fetch = () =>
            {
                return fes.EnumerateDataStores(providerName, partialConnString);
            };
            LogMethodCall("MgFeatureService::EnumerateDataStores", true, providerName, partialConnString);
            return base.DeserializeObject<DataStoreList>(new MgReadOnlyStream(fetch));
        }

        public override string[] GetSchemas(string resourceId)
        {
            List<string> names = new List<string>();
            var fsvc = GetFeatureService();
            var schemaNames = fsvc.GetSchemas(new MgResourceIdentifier(resourceId));
            LogMethodCall("MgFeatureService::GetSchemas", true, resourceId);
            for (int i = 0; i < schemaNames.GetCount(); i++)
            {
                names.Add(schemaNames.GetItem(i));
            }
            return names.ToArray();
        }

        public override string[] GetClassNames(string resourceId, string schemaName)
        {
            List<string> names = new List<string>();
            var fsvc = GetFeatureService();
            var classNames = fsvc.GetClasses(new MgResourceIdentifier(resourceId), schemaName);
            LogMethodCall("MgFeatureService::GetClasses", true, resourceId, schemaName);
            for (int i = 0; i < classNames.GetCount(); i++)
            {
                names.Add(classNames.GetItem(i));
            }
            return names.ToArray();
        }

        protected override ClassDefinition GetClassDefinitionInternal(string resourceId, string schemaName, string className)
        {
            var cls = GetFeatureService().GetClassDefinition(new MgResourceIdentifier(resourceId), schemaName, className);
            return Native.Utility.ConvertClassDefinition(cls);
        }

        public override Version SiteVersion
        {
            get { return new Version(2, 4, 0, 0); }
        }

        public override string[] GetCustomPropertyNames()
        {
            return new string[0];
        }

        public override Type GetCustomPropertyType(string name)
        {
            throw new CustomPropertyNotFoundException();
        }

        public override void SetCustomProperty(string name, object value)
        {
            throw new CustomPropertyNotFoundException();
        }

        public override object GetCustomProperty(string name)
        {
            throw new CustomPropertyNotFoundException();
        }

        public FdoProviderCapabilities GetProviderCapabilities(string provider)
        {
            var fes = GetFeatureService();
            var res = (FdoProviderCapabilities)base.DeserializeObject(typeof(FdoProviderCapabilities), new MgReadOnlyStream(() => fes.GetCapabilities(provider)));
            LogMethodCall("MgFeatureService::GetProviderCapabilities", true, provider);
            return res;
        }

        public string TestConnection(string providername, NameValueCollection parameters)
        {
            var fes = GetFeatureService();
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            if (parameters != null)
            {
                foreach (System.Collections.DictionaryEntry de in parameters)
                    sb.Append((string)de.Key + "=" + (string)de.Value + "\t");
                if (sb.Length > 0)
                    sb.Length--;
            }
            var res = fes.TestConnection(providername, sb.ToString()) ? "True" : "Unspecified errors";
            LogMethodCall("MgFeatureService::TestConnection", true, providername, sb.ToString());
            return res;
        }

        public string[] GetConnectionPropertyValues(string providerName, string propertyName, string partialConnectionString)
        {
            var featSvc = GetFeatureService();
            MgStringCollection result = featSvc.GetConnectionPropertyValues(providerName, propertyName, partialConnectionString);
            LogMethodCall("MgFeatureService::GetConnectionPropertyValues", true, providerName, propertyName, partialConnectionString);
            string[] values = new string[result.GetCount()];
            for (int i = 0; i < result.GetCount(); i++)
            {
                values[i] = result.GetItem(i);
            }
            return values;
        }

        public IReader ExecuteSqlQuery(string featureSourceID, string sql)
        {
            var fes = GetFeatureService();
            MgSqlDataReader reader = fes.ExecuteSqlQuery(new MgResourceIdentifier(featureSourceID), sql);
            LogMethodCall("MgFeatureService::ExecuteSqlQuery", true, featureSourceID, sql);
            return new LocalNativeSqlReader(reader);
        }

        public IFeatureReader QueryFeatureSource(string resourceID, string schema, string query)
        {
            return QueryFeatureSource(resourceID, schema, query, null);
        }

        public IFeatureReader QueryFeatureSource(string resourceID, string schema)
        {
            return QueryFeatureSource(resourceID, schema, null, null);
        }

        public IFeatureReader QueryFeatureSource(string resourceID, string schema, string query, string[] columns)
        {
            return QueryFeatureSource(resourceID, schema, query, columns, null);
        }

        public IFeatureReader QueryFeatureSource(string resourceID, string schema, string query, string[] columns, NameValueCollection computedProperties)
        {
            var fes = GetFeatureService();
            MgFeatureQueryOptions mgf = new MgFeatureQueryOptions();
            if (query != null)
                mgf.SetFilter(query);

            if (columns != null && columns.Length != 0)
                foreach (string s in columns)
                    mgf.AddFeatureProperty(s);

            if (computedProperties != null && computedProperties.Count > 0)
                foreach (string s in computedProperties.Keys)
                    mgf.AddComputedProperty(s, computedProperties[s]);

            var fsId = new MgResourceIdentifier(resourceID);
            MgFeatureReader mr = fes.SelectFeatures(fsId, schema, mgf);

            LogMethodCall("MgFeatureService::SelectFeatures", true, resourceID, schema, "MgFeatureQueryOptions");

            return new LocalNativeFeatureReader(mr);
        }

        public void DeleteResourceData(string resourceID, string dataname)
        {
            var res = GetResourceService();
            res.DeleteResourceData(new MgResourceIdentifier(resourceID), dataname);

            LogMethodCall("MgResourceService::DeleteResourceData", true, resourceID, dataname);
        }

        public ResourceDataList EnumerateResourceData(string resourceID)
        {
            var res = GetResourceService();
            GetByteReaderMethod fetch = () =>
            {
                MgResourceIdentifier resId = new MgResourceIdentifier(resourceID);
                return res.EnumerateResourceData(resId);
            };
            LogMethodCall("MgResourceService::EnumerateResourceData", true, resourceID);
            return base.DeserializeObject<ResourceDataList>(new MgReadOnlyStream(fetch));
        }

        public System.IO.Stream GetTile(string mapdefinition, string baselayergroup, int col, int row, int scaleindex, string format)
        {
            var ts = GetTileService();
            GetByteReaderMethod fetch = () =>
            {
                MgResourceIdentifier mdf = new MgResourceIdentifier(mapdefinition);
                return ts.GetTile(mdf, baselayergroup, col, row, scaleindex);
            };
            LogMethodCall("MgTileService::GetTile", true, mapdefinition, baselayergroup, col.ToString(), row.ToString(), scaleindex.ToString());
            return new MgReadOnlyStream(fetch);
        }

        public System.IO.Stream DescribeDrawing(string resourceID)
        {
            var dwSvc = GetDrawingService();
            GetByteReaderMethod fetch = () =>
            {
                MgResourceIdentifier resId = new MgResourceIdentifier(resourceID);
                return dwSvc.GetDrawing(resId);
            };
            LogMethodCall("MgDrawingService::GetDrawing", true, resourceID);
            return new MgReadOnlyStream(fetch);
        }

        public string[] EnumerateDrawingLayers(string resourceID, string sectionName)
        {
            var dwSvc = GetDrawingService();
            var layers = dwSvc.EnumerateLayers(new MgResourceIdentifier(resourceID), sectionName);
            LogMethodCall("MgDrawingService::EnumerateLayers", true, resourceID, sectionName);
            var layerNames = new List<string>();
            for (int i = 0; i < layers.GetCount(); i++)
            {
                layerNames.Add(layers.GetItem(i));
            }
            return layerNames.ToArray();
        }

        public DrawingSectionResourceList EnumerateDrawingSectionResources(string resourceID, string sectionName)
        {
            var dwSvc = GetDrawingService();
            GetByteReaderMethod fetch = () =>
            {
                MgResourceIdentifier resId = new MgResourceIdentifier(resourceID);
                return dwSvc.EnumerateSectionResources(resId, sectionName);
            };
            LogMethodCall("MgDrawingService::EnumerateDrawingSectionResources", true, resourceID, sectionName);
            return base.DeserializeObject<DrawingSectionResourceList>(new MgReadOnlyStream(fetch));
        }

        public DrawingSectionList EnumerateDrawingSections(string resourceID)
        {
            var dwSvc = GetDrawingService();
            GetByteReaderMethod fetch = () =>
            {
                MgResourceIdentifier resId = new MgResourceIdentifier(resourceID);
                return dwSvc.EnumerateSections(resId);
            };
            LogMethodCall("MgDrawingService::EnumerateDrawingSections", true, resourceID);
            return base.DeserializeObject<DrawingSectionList>(new MgReadOnlyStream(fetch));
        }

        public string GetDrawingCoordinateSpace(string resourceID)
        {
            var dwSvc = GetDrawingService();
            var res = dwSvc.GetCoordinateSpace(new MgResourceIdentifier(resourceID));
            LogMethodCall("MgDrawingService::GetCoordinateSpace", true, resourceID);
            return res;
        }

        public System.IO.Stream GetDrawing(string resourceID)
        {
            var dwSvc = GetDrawingService();
            GetByteReaderMethod fetch = () =>
            {
                MgResourceIdentifier resId = new MgResourceIdentifier(resourceID);
                return dwSvc.GetDrawing(resId);
            };
            LogMethodCall("MgDrawingService::GetDrawing", true, resourceID);
            return new MgReadOnlyStream(fetch);
        }

        public System.IO.Stream GetLayer(string resourceID, string sectionName, string layerName)
        {
            var dwSvc = GetDrawingService();
            GetByteReaderMethod fetch = () =>
            {
                MgResourceIdentifier resId = new MgResourceIdentifier(resourceID);
                return dwSvc.GetLayer(resId, sectionName, layerName);
            };
            LogMethodCall("MgDrawingService::GetLayer", true, resourceID, sectionName, layerName);
            return new MgReadOnlyStream(fetch);
        }

        public System.IO.Stream GetSection(string resourceID, string sectionName)
        {
            var dwSvc = GetDrawingService();
            GetByteReaderMethod fetch = () =>
            {
                MgResourceIdentifier resId = new MgResourceIdentifier(resourceID);
                return dwSvc.GetSection(resId, sectionName);
            };
            LogMethodCall("MgDrawingService::GetSection", true, resourceID, sectionName);
            return new MgReadOnlyStream(fetch);
        }

        public System.IO.Stream GetSectionResource(string resourceID, string resourceName)
        {
            var dwSvc = GetDrawingService();
            GetByteReaderMethod fetch = () =>
            {
                MgResourceIdentifier resId = new MgResourceIdentifier(resourceID);
                return dwSvc.GetSectionResource(resId, resourceName);
            };
            LogMethodCall("MgDrawingService::GetSectionResource", true, resourceID, resourceName);
            return new MgReadOnlyStream(fetch);
        }

        public IFeatureService FeatureService
        {
            get { return this; }
        }

        public IResourceService ResourceService
        {
            get { return this; }
        }

        private IConnectionCapabilities _caps;

        public IConnectionCapabilities Capabilities
        {
            get
            {
                if (_caps == null)
                {
                    _caps = new LocalCapabilities(this);
                }
                return _caps;
            }
        }

        public bool AutoRestartSession
        {
            get;
            set;
        }

        public IService GetService(int serviceType)
        {
            ServiceType st = (ServiceType)serviceType;
            switch (st)
            {
                case ServiceType.Drawing:
                case ServiceType.Feature:
                case ServiceType.Resource:
                case ServiceType.Tile:
                    return this;
            }
            throw new UnsupportedServiceTypeException(st);
        }

        private ICoordinateSystemCatalog m_coordsys = null;

        public OSGeo.MapGuide.MaestroAPI.CoordinateSystem.ICoordinateSystemCatalog CoordinateSystemCatalog
        {
            get 
            {
                if (m_coordsys == null)
                    m_coordsys = new LocalNativeCoordinateSystemCatalog();
                return m_coordsys;
            }
        }

        public string DisplayName
        {
            get { return "Local MgDesktop (" + _configFile + ")"; }
        }

        public void RestartSession()
        {
            
        }

        public bool RestartSession(bool throwException)
        {
            return true;
        }

        internal void InsertFeatures(MgResourceIdentifier fsId, string className, MgPropertyCollection props)
        {
            try
            {
                MgdFeatureService fs = GetFeatureService();
                MgFeatureReader fr = null;
                try
                {
                    fr = fs.InsertFeatures(fsId, className, props);
                }
                finally
                {
                    if (fr != null)
                        fr.Close();
                }
            }
            catch (MgException ex)
            {
                var exMgd = new FeatureServiceException(ex.Message);
                exMgd.MgErrorDetails = ex.GetDetails();
                exMgd.MgStackTrace = ex.GetStackTrace();
                ex.Dispose();
                throw exMgd;
            }
        }

        internal int UpdateFeatures(MgResourceIdentifier fsId, string className, MgPropertyCollection props, string filter)
        {
            try
            {
                MgdFeatureService fs = GetFeatureService();
                return fs.UpdateFeatures(fsId, className, props, filter);
            }
            catch (MgException ex)
            {
                var exMgd = new FeatureServiceException(ex.Message);
                exMgd.MgErrorDetails = ex.GetDetails();
                exMgd.MgStackTrace = ex.GetStackTrace();
                ex.Dispose();
                throw exMgd;
            }
        }

        internal int DeleteFeatures(MgResourceIdentifier fsId, string className, string filter)
        {
            try
            {
                MgdFeatureService fs = GetFeatureService();
                return fs.DeleteFeatures(fsId, className, filter);
            }
            catch (MgException ex)
            {
                var exMgd = new FeatureServiceException(ex.Message);
                exMgd.MgErrorDetails = ex.GetDetails();
                exMgd.MgStackTrace = ex.GetStackTrace();
                ex.Dispose();
                throw exMgd;
            }
        }

        internal void ApplySchema(MgResourceIdentifier fsId, MgFeatureSchema schemaToApply)
        {
            try
            {
                MgdFeatureService fs = GetFeatureService();
                fs.ApplySchema(fsId, schemaToApply);
            }
            catch (MgException ex)
            {
                var exMgd = new FeatureServiceException(ex.Message);
                exMgd.MgErrorDetails = ex.GetDetails();
                exMgd.MgStackTrace = ex.GetStackTrace();
                ex.Dispose();
                throw exMgd;
            }
        }

        internal void CreateDataStore(MgResourceIdentifier fsId, MgFeatureSourceParams fp)
        {
            try
            {
                MgdFeatureService fs = GetFeatureService();
                fs.CreateFeatureSource(fsId, fp);
            }
            catch (MgException ex)
            {
                var exMgd = new FeatureServiceException(ex.Message);
                exMgd.MgErrorDetails = ex.GetDetails();
                exMgd.MgStackTrace = ex.GetStackTrace();
                ex.Dispose();
                throw exMgd;
            }
        }
    }
}
