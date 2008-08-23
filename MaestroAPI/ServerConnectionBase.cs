#region Disclaimer / License
// Copyright (C) 2006, Kenneth Skovhede
// http://www.hexad.dk, opensource@hexad.dk
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
using System.Collections;
using System.Collections.Generic;

namespace OSGeo.MapGuide.MaestroAPI
{
	/// <summary>
	/// Summary description for ServerConnectionBase.
	/// </summary>
	public abstract class ServerConnectionBase
	{
		/// <summary>
		/// A resource lookup table, for converting resourceID's into types.
		/// </summary>
		protected Hashtable m_resourceTypeLookup;

		/// <summary>
		/// A list of cached serializers
		/// </summary>
		protected Hashtable m_serializers;

		/// <summary>
		/// The current XML validator
		/// </summary>
		protected XMLValidator m_validator;

		/// <summary>
		/// The path of Xsd schemas 
		/// </summary>
		protected string m_schemasPath;

		/// <summary>
		/// A lookup table for Xsd Schemas
		/// </summary>
		protected Hashtable m_cachedSchemas;

		/// <summary>
		/// A flag indicating if Xsd validation is perfomed
		/// </summary>
		protected bool m_disableValidation = false;

		/// <summary>
		/// A flag that indicates if a session will be automatically restarted
		/// </summary>
		protected bool m_autoRestartSession = false;

		/// <summary>
		/// A dummy resource, used for the runtime map
		/// </summary>
		internal const string RUNTIMEMAP_XML = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Map></Map>";

		/// <summary>
		/// A dummy resource, used for the runtime map
		/// </summary>
		internal const string RUNTIMEMAP_SELECTION_XML = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Selection></Selection>";

		/// <summary>
		/// The username used to open this connection, if any
		/// </summary>
		protected string m_username;

		/// <summary>
		/// The password used to open this connection, if any
		/// </summary>
		protected string m_password;

        protected UserList m_cachedUserList = null;
        protected GroupList m_cachedGroupList = null;

		protected ServerConnectionBase()
		{
			m_serializers = new Hashtable();
			m_validator = new XMLValidator();
			m_cachedSchemas = new Hashtable();

			m_resourceTypeLookup = new Hashtable();
			m_resourceTypeLookup.Add("LayerDefinition", typeof(LayerDefinition));
			m_resourceTypeLookup.Add("MapDefinition", typeof(MapDefinition));
			m_resourceTypeLookup.Add("WebLayout", typeof(WebLayout));
			m_resourceTypeLookup.Add("FeatureSource", typeof(FeatureSource));
			m_resourceTypeLookup.Add("ApplicationDefinition", typeof(ApplicationDefinition.ApplicationDefinitionType));

			m_schemasPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Schemas");
			m_username = null;
			m_password = null;
		}

		/// <summary>
		/// Gets a full list of resources in the permanent server repository (Library).
		/// This method returns the full catalog and should be used sparringly.
		/// </summary>
		/// <returns>A list of contained resources</returns>
		virtual public ResourceList GetRepositoryResources()
		{
			return GetRepositoryResources("Library://", null, -1);
		}

		/// <summary>
		/// Gets a list of resources in the permanent server repository (Library).
		/// This method limits folder recursion to the specified depth.
		/// </summary>
		/// <param name="depth">The max depth to recurse. Use -1 for no limit.</param>
		/// <returns>A list of contained resources</returns>
		virtual public ResourceList GetRepositoryResources(int depth)
		{
			return GetRepositoryResources("Library://", null, depth);
		}

		/// <summary>
		/// Gets a list of resources in the permanent server repository (Library).
		/// This method limits folder recursion to the specified depth.
		/// </summary>
		/// <param name="startingpoint">The folder from which to return items. Use null for &quot;Library://&quot;</param>
		/// <param name="depth">The max depth to recurse. Use -1 for no limit.</param>
		/// <returns>A list of contained resources</returns>
		virtual public ResourceList GetRepositoryResources(string startingpoint, int depth)
		{
			return GetRepositoryResources(startingpoint, null, depth);
		}

		/// <summary>
		/// Gets a list of resources in the permanent server repository (Library).
		/// This method limits folder recursion to the specified depth.
		/// </summary>
		/// <param name="startingpoint">The folder from which to return items. Use null for &quot;Library://&quot;</param>
		/// <returns>A list of contained resources</returns>
		virtual public ResourceList GetRepositoryResources(string startingpoint)
		{
			return GetRepositoryResources(startingpoint, null, -1);
		}

		/// <summary>
		/// Gets a list of resources in the permanent server repository (Library).
		/// This method limits folder recursion to the specified depth.
		/// </summary>
		/// <param name="startingpoint">The folder from which to return items. Use null for &quot;Library://&quot;</param>
		/// <param name="type">The type of resource to look for. Basically this is the resource extension, like &quot;.MapDefinition&quot;. Use null for all resources.</param>
		/// <returns>A list of contained resources</returns>
		virtual public ResourceList GetRepositoryResources(string startingpoint, string type)
		{
			return GetRepositoryResources(startingpoint, type, -1);
		}

		/// <summary>
		/// Gets a list of resources in the permanent server repository (Library).
		/// This method limits folder recursion to the specified depth.
		/// </summary>
		/// <param name="startingpoint">The folder from which to return items. Use null for &quot;Library://&quot;</param>
		/// <param name="type">The type of resource to look for. Basically this is the resource extension, like &quot;.MapDefinition&quot;. Use null for all resources.</param>
		/// <param name="depth">The max depth to recurse. Use -1 for no limit.</param>
		/// <returns>A list of contained resources</returns>
		abstract public ResourceList GetRepositoryResources(string startingpoint, string type, int depth);

		/// <summary>
		/// Gets the current SessionID.
		/// </summary>
		abstract public string SessionID { get; }

		/// <summary>
		/// Builds a resource Identifier, using path info
		/// </summary>
		/// <param name="path">The initial absolute path, not including type or repository info, ea. Test/MyResource</param>
		/// <param name="type">The type of the resource</param>
		/// <param name="fromSession">True if the item is found in the session</param>
		/// <returns>A string representing the resource identifier</returns>
		virtual public string GetResourceIdentifier(string path, ResourceTypes type,  bool fromSession)
		{
			if (fromSession)
				return "Session:" + SessionID + "//" + path + EnumHelper.ResourceName(type, true);
			else
				return "Library://" + path + EnumHelper.ResourceName(type, true);
		}

		/// <summary>
		/// Validates a resource identifier. Only validates the string, not the existence of the resource
		/// </summary>
		/// <param name="resourceid">The full resource identifier</param>
		/// <param name="type">The type of resource that is identified</param>
		virtual public void ValidateResourceID(string resourceid, ResourceTypes type)
		{
			
		}

        /// <summary>
        /// Deserializes an object from a stream.
        /// </summary>
        /// <typeparam name="T">The expected object type</typeparam>
        /// <param name="data">The stream containing the object</param>
        /// <returns>The deserialized object</returns>
        virtual public T DeserializeObject<T>(System.IO.Stream data)
        {
            return (T)DeserializeObject(typeof(T), data);
        }

		/// <summary>
		/// Deserializes an object from a stream.
		/// </summary>
		/// <param name="type">The expected object type</param>
		/// <param name="data">The stream containing the object</param>
		/// <returns>The deserialized object</returns>
		virtual public object DeserializeObject(Type type, System.IO.Stream data)
		{
			//Must copy stream, because we will be reading it twice :(
			//Once for validation, and once for deserialization
			System.IO.MemoryStream ms = new System.IO.MemoryStream();
			Utility.CopyStream(data, ms);
			ms.Position = 0;

#if DEBUG_LASTMESSAGE
			//Save us a copy for later investigation
			using (System.IO.FileStream fs = System.IO.File.Open("lastResponse.xml", System.IO.FileMode.Create, System.IO.FileAccess.ReadWrite, System.IO.FileShare.None))
				Utility.CopyStream(ms, fs);

			ms.Position = 0;
#endif
			//TODO: Find out why the "xs:include" doesn't work with validator
			//Validation is quite important, as we otherwise may end up injecting malicious code
			//			if (!m_disableValidation)
			//			{
			//				m_validator.Validate(ms, GetSchema(type));
			//				ms.Position = 0;
			//			}

			try
			{
				return GetSerializer(type).Deserialize(ms);
			}
			catch (Exception ex)
			{
				string s = ex.Message;
				throw;
			}
		}

		/// <summary>
		/// Serialize an object into a new memory stream.
		/// </summary>
		/// <param name="o">The object to serialize</param>
		/// <returns>A memorystream with the serialized object</returns>
		virtual public System.IO.MemoryStream SerializeObject(object o)
		{
			System.IO.MemoryStream ms = new System.IO.MemoryStream();
            GetSerializer(o.GetType()).Serialize(new Utf8XmlWriter(ms), o);
            return Utility.RemoveUTF8BOM(ms);
		}

		/// <summary>
		/// Serializes an object into a stream
		/// </summary>
		/// <param name="o">The object to serialize</param>
		/// <param name="stream">The stream to serialize into</param>
		virtual public void SerializeObject(object o, System.IO.Stream stream)
		{
			//The Utf8 writer makes sure the Utf8 tag is in place + sets encoding to Utf8
			//This is needed because the server fails when rendering maps using non utf8 xml documents
			//And the XmlSerializer sytem in .Net does not have a method to set the encoding attribute

            //This does not remove the utf8 BOM marker :(
            //GetSerializer(o.GetType()).Serialize(new Utf8XmlWriter(stream), o);

            SerializeObject(o).WriteTo(stream);
		}

		/// <summary>
		/// Returns an XmlSerializer for the given type
		/// </summary>
		/// <param name="type">The object type to serialize</param>
		/// <returns>An XmlSerializer for the given type</returns>
		virtual protected System.Xml.Serialization.XmlSerializer GetSerializer(Type type)
		{
			if (m_serializers[type] == null)
				m_serializers[type] = new System.Xml.Serialization.XmlSerializer(type);
			return (System.Xml.Serialization.XmlSerializer)m_serializers[type];
		}

		/// <summary>
		/// Gets the highest version the API is currently tested againts
		/// </summary>
		virtual public Version MaxTestedVersion
		{
			get { return SiteVersions.GetVersion(KnownSiteVersions.MapGuideEP2009); }
		}

		/// <summary>
		/// Validates the current server version against the highest tested version.
		/// </summary>
		/// <param name="version">The version to validate</param>
		virtual protected void ValidateVersion(SiteVersion version)
		{
			ValidateVersion(new Version(version.Version));
		}

		/// <summary>
		/// Validates the current server version against the highest tested version.
		/// </summary>
		/// <param name="version">The version to validate</param>
		virtual protected void ValidateVersion(Version version)
		{
			if (version > this.MaxTestedVersion)
				throw new Exception("Untested with MapGuide Build > " + this.MaxTestedVersion.ToString());
		}


		/// <summary>
		/// Returns raw data from the server a byte array
		/// </summary>
		/// <param name="resourceID">The full resourceID to get data from</param>
		/// <returns>Raw data from the given resource</returns>
		abstract public byte[] GetResourceXmlData(string resourceID);

		/// <summary>
		/// Returns a WebLayout from the server.
		/// </summary>
		/// <param name="resourceID">The full resourceID of the WebLayout.</param>
		/// <returns>The WebLayout with the given ResourceID</returns>
		virtual public WebLayout GetWebLayout(string resourceID)
		{
			return (WebLayout) GetResource(resourceID);
		}

		/// <summary>
		/// Returns a MapDefinition from the server.
		/// </summary>
		/// <param name="resourceID">The full resourceID of the MapDefinition.</param>
		/// <returns>The MapDefinition with the given ResourceID</returns>
		virtual public MapDefinition GetMapDefinition(string resourceID)
		{
			return (MapDefinition) GetResource(resourceID);
		}

		/// <summary>
		/// Returns a LayerDefinition from the server.
		/// </summary>
		/// <param name="resourceID">The full resourceID of the LayerDefinition.</param>
		/// <returns>The LayerDefinition with the given ResourceID</returns>
		virtual public LayerDefinition GetLayerDefinition(string resourceID)
		{
			return (LayerDefinition) GetResource(resourceID);
		}

		/// <summary>
		/// Returns a FeatureSource from the server.
		/// </summary>
		/// <param name="resourceID">The full resourceID of the FeatureSource.</param>
		/// <returns>The FeatureSource with the given ResourceID</returns>
		virtual public FeatureSource GetFeatureSource(string resourceID)
		{
			return (FeatureSource) GetResource(resourceID);
		}

		/// <summary>
		/// Returns a ApplicationDefinition from the server.
		/// </summary>
		/// <param name="resourceID">The full resourceID of the ApplicationDefinition.</param>
		/// <returns>The ApplicationDefinition with the given ResourceID</returns>
		virtual public ApplicationDefinition.ApplicationDefinitionType GetApplicationDefinition(string resourceID)
		{
			return (ApplicationDefinition.ApplicationDefinitionType) GetResource(resourceID);
		}

		/// <summary>
		/// Returns an object deserialized from server data.
		/// Uses the ResourceID to infer the object type.
		/// </summary>
		/// <param name="resourceID">The full resourceID of the item to retrieve.</param>
		/// <returns>A deserialized object.</returns>
		virtual public object GetResource(string resourceID)
		{
			Type type = GetResourceType(resourceID);

			object o = DeserializeObject(type, new System.IO.MemoryStream(GetResourceXmlData(resourceID)));

			System.Reflection.MethodInfo mi = type.GetMethod("SetResourceId");
			if (mi != null)
				mi.Invoke(o, new object[] {resourceID});

			mi = type.GetMethod("SetConnection");
			if (mi != null)
				mi.Invoke(o, new object[] {this});

			System.Reflection.PropertyInfo pi = type.GetProperty("ResourceId");
			if (pi != null && pi.CanWrite)
				pi.SetValue(o, resourceID, null);

			pi = type.GetProperty("CurrentConnection");
			if (pi != null && pi.CanWrite)
				pi.SetValue(o, this, null);

			return o;
		}

		/// <summary>
		/// Removes the version numbers from a providername
		/// </summary>
		/// <param name="providername">The name of the provider, with or without version numbers</param>
		/// <returns>The provider name without version numbers</returns>
		virtual public string RemoveVersionFromProviderName(string providername)
		{
			double x;
			string[] parts = providername.Split('.');
			for(int i = parts.Length - 1; i >= 0; i--)
			{
				if (!double.TryParse(parts[i], System.Globalization.NumberStyles.Integer, null, out x))
				{
					if (i != 0)
						return string.Join(".", parts, 0, i + 1);
					break;
				}
			}
			return providername;
		}

		/// <summary>
		/// Gets or sets a hashtable where the key is resourceID extensions, and values are System.Type values.
		/// </summary>
		virtual public Hashtable ResourceTypeLookup
		{
			get { return m_resourceTypeLookup; }
			set { m_resourceTypeLookup = value; }
		}

		/// <summary>
		/// Gets an object type from the resourceID.
		/// Use the ResourceTypeLookup to add or remove types.
		/// </summary>
		/// <param name="resourceID">The resourceID for the resource</param>
		/// <returns>The type of the given item, throws an exception if the type does not exist</returns>
		virtual public Type GetResourceType(string resourceID)
		{
			string extension = resourceID.Substring(resourceID.LastIndexOf(".") + 1);
			return (Type)ResourceTypeLookup[extension];
		}

		/// <summary>
		/// Gets an object type from the resourceID.
		/// Use the ResourceTypeLookup to add or remove types.
		/// </summary>
		/// <param name="resourceID">The resourceID for the resource</param>
		/// <returns>The type of the given item, returns null if no such type exists</returns>
		virtual public Type TryGetResourceType(string resourceID)
		{
			try
			{
				string extension = resourceID.Substring(resourceID.LastIndexOf(".") + 1);
				if (ResourceTypeLookup.ContainsKey(extension))
					return (Type)ResourceTypeLookup[extension];
			}
			catch
			{
			}

			return null;
		}

		/// <summary>
		/// Returns an installed provider, given the name of the provider
		/// </summary>
		/// <param name="providername">The name of the provider</param>
		/// <returns>The first matching provider or null</returns>
		virtual public FeatureProviderRegistryFeatureProvider GetFeatureProvider(string providername)
		{
			string pname = RemoveVersionFromProviderName(providername).ToLower();
			foreach(FeatureProviderRegistryFeatureProvider p in this.FeatureProviders)
				if (RemoveVersionFromProviderName(p.Name).ToLower().Equals(pname.ToLower()))
					return p;

			return null;
		}

		public virtual string TestConnection(FeatureSource feature)
		{
			string f = GetResourceIdentifier(Guid.NewGuid().ToString(), ResourceTypes.FeatureSource, true);

			try
			{
				SaveResourceAs(feature, f);
				/*if (resources != null)
					foreach(Stream s in resources)
						SetResourceData(f, "", "", s);*/
				return TestConnection(f);
			}
			finally
			{
				try { DeleteResource(f); }
				catch {}
			}
		}

		public abstract string TestConnection(string featuresource);
		public abstract void DeleteResource(string resourceid);

		/// <summary>
		/// Writes an object into a resourceID
		/// </summary>
		/// <param name="resourceid">The resource to write into</param>
		/// <param name="resource">The resourcec to write</param>
		virtual public void WriteResource(string resourceid, object resource)
		{
			System.IO.MemoryStream ms = SerializeObject(resource);
			ms.Position = 0;

			//Validate that our data is correctly formated
			/*if (!m_disableValidation)
			{
				m_validator.Validate(ms, GetSchema(resource.GetType()));
				ms.Position = 0;
			}*/

#if DEBUG_LASTMESSAGE
			using (System.IO.Stream s = System.IO.File.Open("lastSave.xml", System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None))
				Utility.CopyStream(ms, s);
			ms.Position = 0;
#endif
		
			SetResourceXmlData(resourceid, ms);
		}

		/// <summary>
		/// Writes raw data into a resource.
		/// </summary>
		/// <param name="resourceid">The resourceID to write into</param>
		/// <param name="stream">The stream containing the data to write.</param>
		abstract public void SetResourceXmlData(string resourceid, System.IO.Stream stream);

		/// <summary>
		/// Gets the Xsd schema for a given type.
		/// </summary>
		/// <param name="type">The type to get the schema for</param>
		/// <returns>The schema for the given type</returns>
		virtual protected System.Xml.Schema.XmlSchema GetSchema(Type type)
		{
			if (m_cachedSchemas[type] == null)
			{
				System.Reflection.FieldInfo fi = type.GetField("SchemaName", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public );
				if (fi == null)
					throw new Exception("Type " + type + ", does not contain Schema Info");

				string xsd = (string)fi.GetValue(null);

				using (System.IO.FileStream fs = System.IO.File.Open(System.IO.Path.Combine(m_schemasPath, xsd), System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
					m_cachedSchemas.Add(type, System.Xml.Schema.XmlSchema.Read(fs, null));
			}

			return (System.Xml.Schema.XmlSchema)m_cachedSchemas[type];
		
		}

		/// <summary>
		/// Gets or sets the collection of cached schemas. Use the object type for key, and an XmlSchema instance for value.
		/// </summary>
		virtual public Hashtable CachedSchemas
		{
			get { return m_cachedSchemas; }
			set { m_cachedSchemas = value; }
		}


		/// <summary>
		/// Returns a boolean indicating if a given resource exists
		/// </summary>
		/// <param name="resourceid">The resource to look for</param>
		/// <returns>True if the resource exists false otherwise. Also returns false on error.</returns>
		public bool ResourceExists(string resourceid)
		{
			try
			{
				string sourcefolder;
				if (resourceid.EndsWith("/"))
					sourcefolder = resourceid.Substring(0, resourceid.Substring(0, resourceid.Length-1).LastIndexOf("/") + 1);
				else
					sourcefolder = resourceid.Substring(0, resourceid.LastIndexOf("/") + 1);

				ResourceList lst = GetRepositoryResources(sourcefolder, 1);
				foreach(object o in lst.Items)
					if (o.GetType() == typeof(ResourceListResourceFolder) && ((ResourceListResourceFolder)o).ResourceId == resourceid)
						return true;
					else if (o.GetType() == typeof(ResourceListResourceDocument) && ((ResourceListResourceDocument)o).ResourceId == resourceid)
						return true;

				return false;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Updates all resource references inside an object.
		/// </summary>
		/// <param name="o">The object in which the resource references are to be updated</param>
		/// <param name="oldresourcepath">The current resource path, the one updating from</param>
		/// <param name="newresourcepath">The new resource path, the one updating to</param>
		/// <param name="folderupdates">True if the old and new resource path identifiers are folders, false otherwise</param>
		public virtual void UpdateResourceReferences(object o, string oldresourcepath, string newresourcepath, bool folderupdates)
		{
			UpdateResourceReferences(o, oldresourcepath, newresourcepath, folderupdates, new Hashtable());
		}

		/// <summary>
		/// Updates all resource references inside an object.
		/// </summary>
		/// <param name="o">The object in which the resource references are to be updated</param>
		/// <param name="oldresourcepath">The current resource path, the one updating from</param>
		/// <param name="newresourcepath">The new resource path, the one updating to</param>
		/// <param name="folderupdates">True if the old and new resource path identifiers are folders, false otherwise</param>
		/// <param name="visited">A hashtable with objects previously visited. Used for recursion, leave as null when calling from outside the API.</param>
		protected void UpdateResourceReferences(object o, string oldresourcepath, string newresourcepath, bool folderupdates, Hashtable visited)
		{
			if (o == null)
				return;

			if (visited == null)
				visited = new Hashtable();

			//Prevent infinite recursion
			if (o as string == null && !o.GetType().IsPrimitive)
			{
				if (visited.ContainsKey(o))
					return;
				else
					visited.Add(o, null);
			}

			if (folderupdates)
			{
				if (!oldresourcepath.EndsWith("/"))
					oldresourcepath += "/";
				if (!newresourcepath.EndsWith("/"))
					newresourcepath += "/";
			}

            //If the value is a document or fragment of a document, we still wan't to repoint it
            if (o as System.Xml.XmlDocument != null || o as System.Xml.XmlNode != null)
            {
                Queue<System.Xml.XmlNode> lst = new Queue<System.Xml.XmlNode>();
                if (o as System.Xml.XmlDocument != null)
                {
                    foreach (System.Xml.XmlNode n in (o as System.Xml.XmlDocument).ChildNodes)
                        if (n.NodeType == System.Xml.XmlNodeType.Element)
                            lst.Enqueue(n);
                }
                else
                    lst.Enqueue(o as System.Xml.XmlNode);

                while (lst.Count > 0)
                {
                    System.Xml.XmlNode n = lst.Dequeue();

                    foreach (System.Xml.XmlNode nx in n.ChildNodes)
                        if (nx.NodeType == System.Xml.XmlNodeType.Element)
                            lst.Enqueue(nx);

                    if (n.Name == "ResourceId")
                    {
                        string current = n.InnerXml;
                        if (folderupdates && current.StartsWith(oldresourcepath))
                            n.InnerXml = newresourcepath + current.Substring(oldresourcepath.Length);
                        else if (current == oldresourcepath)
                            n.InnerXml = newresourcepath;
                    }

                    foreach(System.Xml.XmlAttribute a in n.Attributes)
                        if (a.Name == "ResourceId")
                        {
                            string current = a.Value;
                            if (folderupdates && current.StartsWith(oldresourcepath))
                                n.Value = newresourcepath + current.Substring(oldresourcepath.Length);
                            else if (current == oldresourcepath)
                                n.Value = newresourcepath;
                        }
                }

                //There can be no objects in an xml document or node, so just return immediately
                return;
            }

            //Try to find the object properties
			foreach(System.Reflection.PropertyInfo pi in o.GetType().GetProperties())
			{
                //Only index free read-write properties are taken into account
				if (!pi.CanRead || !pi.CanWrite || pi.GetIndexParameters().Length != 0 || pi.GetValue(o, null) == null)
					continue;

                //If we are at a ResourceId property, update it as needed
				if (pi.Name == "ResourceId")
				{
					object v = pi.GetValue(o, null);
					string current = v as string;

					if (current != null)
					{
						if (folderupdates && current.StartsWith(oldresourcepath))
							pi.SetValue(o, newresourcepath + current.Substring(oldresourcepath.Length), null);
						else if (current == oldresourcepath)
							pi.SetValue(o, newresourcepath, null); 
					}
				}
				else if (pi.GetValue(o, null).GetType().GetInterface(typeof(System.Collections.ICollection).FullName) != null)
				{
                    //Handle collections
					System.Collections.ICollection srcList = (System.Collections.ICollection)pi.GetValue(o, null);
					foreach(object ox in srcList)
						UpdateResourceReferences(ox, oldresourcepath, newresourcepath, folderupdates, visited);
				}
				else if (pi.GetValue(o, null).GetType().IsArray)
				{
                    //Handle arrays
					System.Array sourceArr = (System.Array)pi.GetValue(o, null);
					for(int i = 0; i < sourceArr.Length; i++)
						UpdateResourceReferences(sourceArr.GetValue(i), oldresourcepath, newresourcepath, folderupdates, visited);
				}
                else if (pi.PropertyType.IsClass)
                {
                    //Handle subobjects
                    UpdateResourceReferences(pi.GetValue(o, null), oldresourcepath, newresourcepath, folderupdates, visited);
                }
			}

		}


		/// <summary>
		/// Moves a resource, and subsequently updates all resources pointing to the old resource path
		/// </summary>
		/// <param name="oldpath">The current resource path, the one moving from</param>
		/// <param name="newpath">The new resource path, the one moving to</param>
		/// <param name="callback">A callback delegate, being called for non progress reporting events.</param>
		/// <param name="progress">A callback delegate, being called for progress reporting events.</param>
		/// <returns></returns>
		public bool MoveResourceWithReferences(string oldpath, string newpath, LengthyOperationCallBack callback, LengthyOperationProgressCallBack progress)
		{
			LengthyOperationProgressArgs la = new LengthyOperationProgressArgs("Moving resource...", -1);

			if (progress != null)
				progress(this, la);
			if (la.Cancel)
				return false;
			MoveResource(oldpath, newpath, true);
			la.Progress = 100;
			if (progress != null)
				progress(this, la);
			if (la.Cancel)
				return false;

			la.Progress = -1;
			if (progress != null)
				progress(this, la);
			if (la.Cancel)
				return false;

			ArrayList items = new ArrayList();
			Hashtable paths = new Hashtable();

			//The old path does not exist, but luckily the call works anyway
			ResourceReferenceList rlf = EnumerateResourceReferences(oldpath);

			foreach(string s in rlf.ResourceId)
				if (!paths.ContainsKey(s))
				{
					items.Add(new LengthyOperationCallbackArgs.LengthyOperationItem(s));
					paths.Add(s, null);
				}

			la.Progress = 100;
			if (progress != null)
				progress(this, la);
			if (la.Cancel)
				return false;

			LengthyOperationCallbackArgs args = new LengthyOperationCallbackArgs((LengthyOperationCallbackArgs.LengthyOperationItem[])items.ToArray(typeof(LengthyOperationCallbackArgs.LengthyOperationItem)));

			if (callback != null)
				callback(this, args);

			if (args.Cancel)
				return false;

			if (args.Index > args.Items.Length)
				return true;

			if (args.Items.Length == 0)
				return true;

			do
			{
				LengthyOperationCallbackArgs.LengthyOperationItem item = args.Items[args.Index];
				item.Status = LengthyOperationCallbackArgs.LengthyOperationItem.OperationStatus.Pending;

				if (callback != null)
				{
					callback(this, args);
					if (args.Cancel) return false;
				}

				try
				{
					object o = GetResource(item.Itempath);

					UpdateResourceReferences(o, oldpath, newpath, false);

					SaveResourceAs(o, item.Itempath);
					item.Status = LengthyOperationCallbackArgs.LengthyOperationItem.OperationStatus.Success;
				}
				catch (Exception ex)
				{
					string s = ex.Message;
					item.Status = LengthyOperationCallbackArgs.LengthyOperationItem.OperationStatus.Failure;
				}

				if (callback != null)
				{
					callback(this, args);
					if (args.Cancel) return false;
				}
				
				args.Index++;
			} while(!args.Cancel && args.Index < args.Items.Length);

			return !args.Cancel;
		}

		/// <summary>
		/// Moves a folder, and subsequently updates all resources pointing to the old resource path
		/// </summary>
		/// <param name="oldpath">The current folder path, the one moving from</param>
		/// <param name="newpath">The new folder path, the one moving to</param>
		/// <param name="callback">A callback delegate, being called for non progress reporting events.</param>
		/// <param name="progress">A callback delegate, being called for progress reporting events.</param>
		/// <returns></returns>
		public bool MoveFolderWithReferences(string oldpath, string newpath, LengthyOperationCallBack callback, LengthyOperationProgressCallBack progress)
		{
			oldpath = FixAndValidateFolderPath(oldpath);
			newpath = FixAndValidateFolderPath(newpath);

			LengthyOperationProgressArgs la = new LengthyOperationProgressArgs("Moving folder...", -1);

			if (progress != null)
				progress(this, la);
			if (la.Cancel)
				return false;

			MoveFolder(oldpath, newpath, true);
			la.Progress = 100;
			if (progress != null)
				progress(this, la);
			if (la.Cancel)
				return false;

			int pg = 0;
			la.Progress = 0;
			la.StatusMessage = "Finding folder references...";
			if (progress != null)
				progress(this, la);
			if (la.Cancel)
				return false;

			ResourceList lst = GetRepositoryResources(newpath);

			Hashtable items = new Hashtable();
			foreach(object o in lst.Items) 
			{
				if (o.GetType() == typeof(ResourceListResourceDocument))
				{
					//The old path does not exist, but we need to enumerate references at the old location
					string resource_oldpath = ((ResourceListResourceDocument)o).ResourceId;
					resource_oldpath = oldpath + resource_oldpath.Substring(newpath.Length);

					ResourceReferenceList rlf = EnumerateResourceReferences(resource_oldpath);
					foreach(string s in rlf.ResourceId)
						if (!items.Contains(s))
							items.Add(s, new LengthyOperationCallbackArgs.LengthyOperationItem(s));
				}

				pg++;
				la.Progress = Math.Max(Math.Min(99,  (int)(((double)pg / (double)lst.Items.Count) * (double)100)), 0);

				if (progress != null)
					progress(this, la);
				if (la.Cancel)
					return false;
			}

			la.Progress = 100;
			if (progress != null)
				progress(this, la);
			if (la.Cancel)
				return false;

			LengthyOperationCallbackArgs.LengthyOperationItem[] vi = new LengthyOperationCallbackArgs.LengthyOperationItem[items.Values.Count];
			items.Values.CopyTo(vi, 0);
			LengthyOperationCallbackArgs args = new LengthyOperationCallbackArgs(vi);

			if (callback != null)
				callback(this, args);

			if (args.Cancel)
				return false;

			if (args.Index > args.Items.Length)
				return true;

			if (args.Items.Length == 0)
				return true;

			do
			{
				LengthyOperationCallbackArgs.LengthyOperationItem item = args.Items[args.Index];
				item.Status = LengthyOperationCallbackArgs.LengthyOperationItem.OperationStatus.Pending;

				if (callback != null)
				{
					callback(this, args);
					if (args.Cancel) 
						return false;
				}

				try
				{
					object o = GetResource(item.Itempath);

					UpdateResourceReferences(o, oldpath, newpath, true);

					SaveResourceAs(o, item.Itempath);
					item.Status = LengthyOperationCallbackArgs.LengthyOperationItem.OperationStatus.Success;
				}
				catch (Exception ex)
				{
					string s = ex.Message;
					item.Status = LengthyOperationCallbackArgs.LengthyOperationItem.OperationStatus.Failure;
				}

				if (callback != null)
				{
					callback(this, args);
					if (args.Cancel) 
						return false;
				}
				
				args.Index++;
			} while(!args.Cancel && args.Index < args.Items.Length);

			return !args.Cancel;
		}

		/// <summary>
		/// Copies folder, and subsequently updates all resources within the folder to use the new folder path instead of the originating one.
		/// </summary>
		/// <param name="oldpath">The current folder path, the one copying from</param>
		/// <param name="newpath">The new folder path, the one copying to</param>
		/// <param name="callback">A callback delegate, being called for non progress reporting events.</param>
		/// <param name="progress">A callback delegate, being called for progress reporting events.</param>
		/// <returns></returns>
		public bool CopyFolderWithReferences(string oldpath, string newpath, LengthyOperationCallBack callback, LengthyOperationProgressCallBack progress)
		{
			oldpath = FixAndValidateFolderPath(oldpath);
			newpath = FixAndValidateFolderPath(newpath);
			ResourceList lst = GetRepositoryResources(oldpath);

			LengthyOperationProgressArgs la = new LengthyOperationProgressArgs("Copying folder...", -1);
			if (progress != null)
				progress(this, la);
			if (la.Cancel)
				return false;
			CopyFolder(oldpath, newpath, true);
			la.Progress = 100;
			if (progress != null)
				progress(this, la);
			if (la.Cancel)
				return false;


			la.Progress = 0;
			la.StatusMessage = "Finding folder references...";
			int pg = 0;
			if (progress != null)
				progress(this, la);
			if (la.Cancel)
				return false;
			ArrayList items = new ArrayList();
			Hashtable paths = new Hashtable();
			foreach(object o in lst.Items)
			{
				if (o.GetType() == typeof(ResourceListResourceDocument))
				{
					ResourceReferenceList rlf = EnumerateResourceReferences(((ResourceListResourceDocument)o).ResourceId);
					foreach(string s in rlf.ResourceId)
						if (s.StartsWith(oldpath))
						{
							string dest = newpath + s.Substring(oldpath.Length);
							if (!paths.ContainsKey(dest))
							{
								items.Add(new LengthyOperationCallbackArgs.LengthyOperationItem(dest));
								paths.Add(dest, null);
							}
						}
				}
				pg++;
				la.Progress = Math.Max(Math.Min(99,  (int)(((double)pg / (double)lst.Items.Count) * (double)100)), 0);

				if (progress != null)
					progress(this, la);
				if (la.Cancel)
					return false;
			}
			
			la.Progress = 100;
			if (progress != null)
				progress(this, la);
			if (la.Cancel)
				return false;

			LengthyOperationCallbackArgs args = new LengthyOperationCallbackArgs((LengthyOperationCallbackArgs.LengthyOperationItem[])items.ToArray(typeof(LengthyOperationCallbackArgs.LengthyOperationItem)));

			if (callback != null)
				callback(this, args);

			if (args.Cancel)
				return false;

			if (args.Index > args.Items.Length)
				return true;

			if (args.Items.Length == 0)
				return true;

			do
			{
				LengthyOperationCallbackArgs.LengthyOperationItem item = args.Items[args.Index];
				item.Status = LengthyOperationCallbackArgs.LengthyOperationItem.OperationStatus.Pending;

				if (callback != null)
				{
					callback(this, args);
					if (args.Cancel) return false;
				}

				try
				{
					object o = GetResource(item.Itempath);

					UpdateResourceReferences(o, oldpath, newpath, true);

					SaveResourceAs(o, item.Itempath);
					item.Status = LengthyOperationCallbackArgs.LengthyOperationItem.OperationStatus.Success;
				}
				catch (Exception ex)
				{
					string s = ex.Message;
					item.Status = LengthyOperationCallbackArgs.LengthyOperationItem.OperationStatus.Failure;
				}

				if (callback != null)
				{
					callback(this, args);
					if (args.Cancel) return false;
				}
				
				args.Index++;
			} while(!args.Cancel && args.Index < args.Items.Length);

			return !args.Cancel;
		}


		/// <summary>
		/// Validates the origin of the folder, and ensures the folder path has a trailing slash.
		/// </summary>
		/// <param name="folderpath">The path to validate and fix</param>
		/// <returns>The fixed path</returns>
		virtual protected string FixAndValidateFolderPath(string folderpath)
		{
			if (! folderpath.StartsWith("Library://") && ! folderpath.StartsWith("Session:" + this.SessionID + "//"))
				throw new Exception("Invalid folder path, must be either library or session");

			if (!folderpath.EndsWith("/"))
				folderpath += "/";

			return folderpath;
		}

		/// <summary>
		/// Creates a folder on the server
		/// </summary>
		/// <param name="resourceID">The path of the folder to create</param>
		virtual public void CreateFolder(string resourceID)
		{
			resourceID = FixAndValidateFolderPath(resourceID);
			SetResourceXmlData(resourceID, new System.IO.MemoryStream());
		}

		/// <summary>
		/// Returns a value indicating if a given folder exists
		/// </summary>
		/// <param name="folderpath">The path of the folder</param>
		/// <returns>True if the folder exists, false otherwise. Also returns false on error.</returns>
		virtual public bool HasFolder(string folderpath)
		{
			folderpath = FixAndValidateFolderPath(folderpath);

			try
			{
				ResourceList l = this.GetRepositoryResources(folderpath, 1);
				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Enumereates all references to a given resource
		/// </summary>
		/// <param name="resourceid">The resource to enumerate references for</param>
		/// <returns>A list of resources that reference the given resourceID</returns>
		abstract public ResourceReferenceList EnumerateResourceReferences(string resourceid);

		/// <summary>
		/// Copies a resource from one location to another. This does not update any references.
		/// </summary>
		/// <param name="oldpath">The current resource path, the one copying from</param>
		/// <param name="newpath">The new resource path, the one copying to</param>
		/// <param name="overwrite">True if the copy can overwrite an existing resource, false otherwise</param>
		abstract public void CopyResource(string oldpath, string newpath, bool overwrite);

		/// <summary>
		/// Copies a folder and all its content. This does not update any references.
		/// </summary>
		/// <param name="oldpath">The current folder path, the one copying from</param>
		/// <param name="newpath">The new folder path, the one copying to</param>
		/// <param name="overwrite">True if the copy can overwrite an existing folder, false otherwise</param>
		abstract public void CopyFolder(string oldpath, string newpath, bool overwrite);

		/// <summary>
		/// Moves a resource from one location to another. This does not update any references.
		/// </summary>
		/// <param name="oldpath">The current resource path, the one moving from</param>
		/// <param name="newpath">The new resource path, the one moving to</param>
		/// <param name="overwrite">True if the move can overwrite an existing resource, false otherwise</param>
		abstract public void MoveResource(string oldpath, string newpath, bool overwrite);

		/// <summary>
		/// Moves a folder and its content from one location to another. This does not update any references.
		/// </summary>
		/// <param name="oldpath">The current folder path, the one moving from</param>
		/// <param name="newpath">The new folder path, the one moving to</param>
		/// <param name="overwrite">True if the move can overwrite an existing folder, false otherwise</param>
		abstract public void MoveFolder(string oldpath, string newpath, bool overwrite);


		/// <summary>
		/// Returns data from a resource as a memorystream
		/// </summary>
		/// <param name="resourceID">The id of the resource to fetch data from</param>
		/// <param name="dataname">The name of the associated data item</param>
		/// <returns>A stream containing the references resource data</returns>
		abstract public System.IO.MemoryStream GetResourceData(string resourceID, string dataname);

        /// <summary>
        /// Uploads data to a resource
        /// </summary>
        /// <param name="resourceid">The id of the resource to update</param>
        /// <param name="dataname">The name of the data to update or create</param>
        /// <param name="datatype">The type of data</param>
        /// <param name="stream">A stream containing the new content of the resource data</param>
        virtual public void SetResourceData(string resourceid, string dataname, ResourceDataType datatype, System.IO.Stream stream)
        {
            SetResourceData(resourceid, dataname, datatype, stream, null);
        }
        
        /// <summary>
		/// Uploads data to a resource
		/// </summary>
		/// <param name="resourceid">The id of the resource to update</param>
		/// <param name="dataname">The name of the data to update or create</param>
		/// <param name="datatype">The type of data</param>
		/// <param name="stream">A stream containing the new content of the resource data</param>
		abstract public void SetResourceData(string resourceid, string dataname, ResourceDataType datatype, System.IO.Stream stream, Utility.StreamCopyProgressDelegate callback);

		/// <summary>
		/// Saves a WebLayout, using its originating resourceId
		/// </summary>
		/// <param name="resource">The WebLayout to save</param>
		public void SaveResource(WebLayout resource)
		{
			if (resource.ResourceId == null)
				throw new Exception("Cannot save weblayout when no resourceid has been set, use SaveAs instead");

			SaveResourceAs(resource, resource.ResourceId);
		}

		/// <summary>
		/// Saves a FeatureSource, using its originating resourceId
		/// </summary>
		/// <param name="resource">The FeatureSource to save</param>
		public void SaveResource(FeatureSource resource)
		{
			if (resource.ResourceId == null)
				throw new Exception("Cannot save featuresource when no resourceid has been set, use SaveAs instead");

			SaveResourceAs(resource, resource.ResourceId);
		}

		/// <summary>
		/// Saves a LayerDefinition, using its originating resourceId
		/// </summary>
		/// <param name="resource">The LayerDefinition to save</param>
		public void SaveResource(LayerDefinition resource)
		{
			if (resource.ResourceId == null)
				throw new Exception("Cannot save layer when no resourceid has been set, use SaveAs instead");

			SaveResourceAs(resource, resource.ResourceId);
		}

		/// <summary>
		/// Saves a MapDefinition, using its originating resourceId
		/// </summary>
		/// <param name="resource">The MapDefintion to save</param>
		public void SaveResource(MapDefinition resource)
		{
			if (resource.ResourceId == null)
				throw new Exception("Cannot save map when no resourceid has been set, use SaveAs instead");

			SaveResourceAs(resource, resource.ResourceId);
		}

		/// <summary>
		/// Saves an object into the repository
		/// </summary>
		/// <param name="resource">The object to save</param>
		/// <param name="resourceid">The resourceId to save the object as</param>
		public void SaveResourceAs(object resource, string resourceid)
		{
			LayerDefinition ldef = resource as LayerDefinition;
			if (ldef != null)
			{
				if (ldef.version == "1.1.0" && this.SiteVersion <= SiteVersions.GetVersion(KnownSiteVersions.MapGuideEP1_1))
					ldef.version = "1.0.0";

				bool version1 = ldef.version == "1.0.0";

				VectorLayerDefinitionType vldef = ldef.Item as VectorLayerDefinitionType;
				if (vldef != null)
				{
					if (vldef.ToolTip == null)
						vldef.ToolTip = "";
					if (vldef.Url == null)
						vldef.Url = "";
					if (vldef.Filter == null)
						vldef.Filter = "";
					foreach(VectorScaleRangeType vsc in vldef.VectorScaleRange)
					{
						for(int i= vsc.Items.Count - 1; i >= 0; i--)
						{
							object o = vsc.Items[i];
							if (o as PointTypeStyleType != null)
							{
								if (((PointTypeStyleType)vsc.Items[i]).PointRule != null)
								{
									if (((PointTypeStyleType)vsc.Items[i]).PointRule.Count == 0 )
										vsc.Items.RemoveAt(i);
									else
										foreach(PointRuleType pr in ((PointTypeStyleType)vsc.Items[i]).PointRule)
										{
											if (pr.LegendLabel == null)
												pr.LegendLabel = "";
											if (pr.Item != null && pr.Item.Item != null)
											{
												if (pr.Item.Item as MarkSymbolType != null)
												{
													MarkSymbolType mks = pr.Item.Item as MarkSymbolType;
													if (mks.Edge != null)
													{
														if (version1)
															mks.Edge.SizeContext = SizeContextType.Default;
														else if (mks.Edge.SizeContext == SizeContextType.Default)
															mks.Edge.SizeContext = SizeContextType.MappingUnits;
													}

													if (mks.Fill != null)
													{
													}

                                                    if (!mks.InsertionPointXSpecified)
                                                    {
                                                        mks.InsertionPointX = 0;
                                                        mks.InsertionPointXSpecified = true;
                                                    }

                                                    if (!mks.InsertionPointYSpecified)
                                                    {
                                                        mks.InsertionPointY = 0;
                                                        mks.InsertionPointYSpecified = true;
                                                    }

                                                    if (!mks.MaintainAspectSpecified)
                                                    {
                                                        mks.MaintainAspect = true;
                                                        mks.MaintainAspectSpecified = true;
                                                    }

                                                    if (mks.SizeX == null || mks.SizeY == null)
                                                        mks.Unit = LengthUnitType.Points;

                                                    if (mks.SizeX == null)
                                                        mks.SizeX = "10";
                                                    if (mks.SizeY == null)
                                                        mks.SizeY = "10";

                                                    if (mks.SizeContext == SizeContextType.Default)
                                                        mks.SizeContext = SizeContextType.DeviceUnits;

												}
												/*if (version1)
													pr.Item.Item.SizeContext = SizeContextType.Default;
												else*/ if (pr.Item.Item.SizeContext == SizeContextType.Default)
														   pr.Item.Item.SizeContext = SizeContextType.MappingUnits;
											}
										}
								}
							}
							else if (o as LineTypeStyleType != null)
							{
                                if (((LineTypeStyleType)vsc.Items[i]).LineRule != null)
                                {
                                    if (((LineTypeStyleType)vsc.Items[i]).LineRule.Count == 0)
                                        vsc.Items.RemoveAt(i);
                                    else
                                        foreach (LineRuleType lr in ((LineTypeStyleType)vsc.Items[i]).LineRule)
                                        {
                                            if (lr.LegendLabel == null)
                                                lr.LegendLabel = "";
                                            if (lr.Items != null)
                                            {
                                                foreach (StrokeType st in lr.Items)
                                                {
                                                    if (version1)
                                                        st.SizeContext = SizeContextType.Default;
                                                    else if (st.SizeContext == SizeContextType.Default)
                                                        st.SizeContext = SizeContextType.MappingUnits;

                                                    if (string.IsNullOrEmpty(st.Thickness))
                                                        st.Thickness = "0";
                                                }
                                            }
                                        }
                                }
                                else
                                    vsc.Items.RemoveAt(i);
							}
							else if (o as AreaTypeStyleType != null)
							{
								if (((AreaTypeStyleType)vsc.Items[i]).AreaRule != null && ((AreaTypeStyleType)vsc.Items[i]).AreaRule.Count == 0 )
									vsc.Items.RemoveAt(i);

								foreach(AreaRuleType ar in ((AreaTypeStyleType)vsc.Items[i]).AreaRule)
								{
									if (ar.LegendLabel == null)
										ar.LegendLabel = "";
									if (ar.Item != null && ar.Item.Stroke != null)
									{
										if (version1)
											ar.Item.Stroke.SizeContext = SizeContextType.Default;
										else if (ar.Item.Stroke.SizeContext == SizeContextType.Default)
											ar.Item.Stroke.SizeContext = SizeContextType.MappingUnits;
                                    
                                        if (string.IsNullOrEmpty(ar.Item.Stroke.Thickness))
                                            ar.Item.Stroke.Thickness = "0";
                                    }
								}

							}
						}
					}
				}

				GridLayerDefinitionType gld = ldef.Item as GridLayerDefinitionType;
				if (gld != null)
				{
					if (gld.GridScaleRange != null)
						foreach(GridScaleRangeType gs in gld.GridScaleRange)
							if (gs.ColorStyle != null && gs.ColorStyle.ColorRule != null)
								foreach(GridColorRuleType gc in gs.ColorStyle.ColorRule)
									if (gc.LegendLabel == null)
										gc.LegendLabel = "";
				}

				//TODO: Clear any data that is not supported under the current version
				if (ldef.version == "1.0.0")
				{}
				else if (ldef.version == "1.1.0")
				{}
			}

			MapDefinition mdef = resource as MapDefinition;
			if (mdef != null)
			{
				if (mdef.BaseMapDefinition != null)
					if (mdef.BaseMapDefinition.BaseMapLayerGroup == null)
						mdef.BaseMapDefinition = null;
					else if (mdef.BaseMapDefinition.BaseMapLayerGroup.Count == 0)
						mdef.BaseMapDefinition = null;

				if (mdef.Layers != null)
                    foreach (MapLayerType mlt in mdef.Layers)
                    {
                        if (mlt.LegendLabel == null)
                            mlt.LegendLabel = "";
                        if (mlt.Group == null)
                            mlt.Group = "";
                    }

				if (mdef.LayerGroups != null)
                    foreach (MapLayerGroupType mlg in mdef.LayerGroups)
                    {
                        if (mlg.LegendLabel == null)
                            mlg.LegendLabel = "";
                        if (mlg.Group == null)
                            mlg.Group = "";
                    }
			}

			WebLayout wl = resource as WebLayout;
			if (wl != null)
			{
				if (wl.CommandSet != null)
					foreach(CommandType cmd in wl.CommandSet)
					{
						if (cmd.Name == null)
							cmd.Name = "";
						if (cmd.Label == null)
							cmd.Label = "";

						/*if (cmd as TargetedCommandType != null)
							if ((cmd as TargetedCommandType).TargetFrame == null)
								(cmd as TargetedCommandType).TargetFrame = "";*/

						if (cmd as SearchCommandType != null)
						{
							SearchCommandType sc = cmd as SearchCommandType;
							if (sc.Layer == null)
								sc.Layer = "";
							if (sc.Prompt == null)
								sc.Prompt = "";

							if (sc.ResultColumns != null && sc.ResultColumns.Count == 0)
								sc.ResultColumns = null;
						}

						if (cmd as InvokeURLCommandType != null)
						{
							InvokeURLCommandType sc = cmd as InvokeURLCommandType;
							if (sc.URL == null)
								sc.URL = "";
						}

						if (cmd as InvokeScriptCommandType != null)
						{
							InvokeScriptCommandType sc = cmd as InvokeScriptCommandType;
							if (sc.Script == null)
								sc.Script = "";
						}


					}

			}


			//TODO: Extract ResourceType from resource.GetType(), and validate
			//ValidateResourceID(resourceid, ResourceTypes.MapDefinition);
			System.IO.MemoryStream ms = SerializeObject(resource);
			SetResourceXmlData(resourceid, ms);
		}

		/// <summary>
		/// Saves a MapDefinition under a different resourceID
		/// </summary>
		/// <param name="resource">The MapDefinition to save</param>
		/// <param name="resourceid">The new path of the MapDefinition</param>
		virtual public void SaveResourceAs(MapDefinition resource, string resourceid)
		{
			SaveResourceAs((object)resource, resourceid);
		}

		/// <summary>
		/// Saves a LayerDefinition under a different resourceID
		/// </summary>
		/// <param name="resource">The LayerDefinition to save</param>
		/// <param name="resourceid">The new path of the LayerDefinition</param>
		virtual public void SaveResourceAs(LayerDefinition resource, string resourceid)
		{
			SaveResourceAs((object)resource, resourceid);
		}

		/// <summary>
		/// Saves a FeatureSource under a different resourceID
		/// </summary>
		/// <param name="resource">The FeatureSource to save</param>
		/// <param name="resourceid">The new path of the FeatureSource</param>
		virtual public void SaveResourceAs(FeatureSource resource, string resourceid)
		{
			SaveResourceAs((object)resource, resourceid);
		}

		/// <summary>
		/// Saves a WebLayout under a different resourceID
		/// </summary>
		/// <param name="resource">The WebLayout to save</param>
		/// <param name="resourceid">The new path of the WebLayout</param>
		virtual public void SaveResourceAs(WebLayout resource, string resourceid)
		{
			SaveResourceAs((object)resource, resourceid);
		}

		/// <summary>
		/// Gets a list of installed feature providers
		/// </summary>
		abstract public FeatureProviderRegistryFeatureProviderCollection FeatureProviders { get; }

		/// <summary>
		/// Gets or sets a flag that indicates if the Xml resources are validated before leaving and entering the server.
		/// </summary>
		public bool DisableValidation 
		{ 
			get {return m_disableValidation; } 
			set { m_disableValidation = value; }
		}

		/// <summary>
		/// Gets the name of a resource, given its identifier
		/// </summary>
		/// <param name="identifier">The identifier to look for</param>
		/// <param name="includePath">True if path information should be include, false otherwise</param>
		/// <returns>The name of the resource</returns>
		public string GetResourceName(string identifier, bool includePath)
		{

			int begin;
			if (includePath)
			{
				if (identifier.IndexOf("//") < 0)
					throw new Exception("Not valid identifier");

				begin = identifier.IndexOf("//") + 2;
			}
			else
			{
				if (identifier.IndexOf("/") < 0)
					throw new Exception("Not valid identifier");

				begin = identifier.LastIndexOf("/") + 1;
			}

			int end = identifier.LastIndexOf(".");
			if (end < begin)
				throw new Exception("Not valid identifier");
			return identifier.Substring(begin, end - begin);
		}

		/// <summary>
		/// Gets or sets a value indicating if the session should automatically be restarted if it expires
		/// </summary>
		virtual public bool AutoRestartSession 
		{ 
			get { return m_autoRestartSession; }
			set { m_autoRestartSession = value; }
		}


		abstract public Version SiteVersion { get; }

		/// <summary>
		/// Determines if an exception is a "Session Expired" exception.
		/// </summary>
		/// <param name="ex">The exception to evaluate</param>
		/// <returns>True if the exception is a session expired exception</returns>
		abstract public bool IsSessionExpiredException(Exception ex);

		/// <summary>
		/// Returns the avalible application templates on the server
		/// </summary>
		/// <returns>The avalible application templates on the server</returns>
		abstract public ApplicationDefinitionTemplateInfoSet GetApplicationTemplates();

		/// <summary>
		/// Returns the avalible application widgets on the server
		/// </summary>
		/// <returns>The avalible application widgets on the server</returns>
		abstract public ApplicationDefinitionWidgetInfoSet GetApplicationWidgets();

		/// <summary>
		/// Returns the avalible widget containers on the server
		/// </summary>
		/// <returns>The avalible widget containers on the server</returns>
		abstract public ApplicationDefinitionContainerInfoSet GetApplicationContainers();

		/// <summary>
		/// Returns the spatial info for a given featuresource
		/// </summary>
		/// <param name="resourceID">The ID of the resource to query</param>
		/// <param name="activeOnly">Query only active items</param>
		/// <returns>A list of spatial contexts</returns>
		abstract public FdoSpatialContextList GetSpatialContextInfo(string resourceID, bool activeOnly);

		/// <summary>
		/// Gets the names of the identity properties from a feature
		/// </summary>
		/// <param name="resourceID">The resourceID for the FeatureSource</param>
		/// <param name="classname">The classname of the feature, including schema</param>
		/// <returns>A string array with the found identities</returns>
		abstract public string[] GetIdentityProperties(string resourceID, string classname);

		/// <summary>
		/// Restarts the server session, and creates a new session ID
		/// </summary>
		public void RestartSession()
		{
			RestartSession(true);
		}

		/// <summary>
		/// Restarts the server session, and creates a new session ID
		/// </summary>
		/// <param name="throwException">If set to true, the call throws an exception if the call failed</param>
		/// <returns>True if the creation succeed, false otherwise</returns>
		abstract public bool RestartSession(bool throwException);

		/// <summary>
		/// Sets the selection of a map
		/// </summary>
		/// <param name="runtimeMap">The resourceID of the runtime map</param>
		/// <param name="selectionXml">The selection xml</param>
		abstract public void SetSelectionXml(string runtimeMap, string selectionXml);

		/// <summary>
		/// Gets the selection from a map
		/// </summary>
		/// <param name="runtimeMap">The resourceID of the runtime map</param>
		/// <returns>The selection xml</returns>
		abstract public string GetSelectionXml(string runtimeMap);

		/// <summary>
		/// Enumerates all unmanaged folders, meaning alias'ed folders
		/// </summary>
		/// <param name="type">The type of data to return</param>
		/// <param name="filter">A filter applied to the items</param>
		/// <param name="recursive">True if the list should contains recursive results</param>
		/// <param name="startpath">The path to retrieve the data from</param>
		/// <returns>A list of unmanaged data</returns>
		abstract public UnmanagedDataList EnumerateUnmanagedData(string startpath, string filter, bool recursive, UnmanagedDataTypes type);

        abstract public void Dispose();

		/// <summary>
		/// Selects features from a runtime map, returning a selection Xml.
		/// </summary>
		/// <param name="runtimemap">The map to query. NOT a resourceID, only the map name!</param>
		/// <param name="wkt">The WKT of the geometry to query with (always uses intersection)</param>
		/// <param name="persist">True if the selection should be saved in the runtime map, false otherwise.</param>
		/// <returns>The selection Xml, or an empty string if there were no data.</returns>
		public string QueryMapFeatures(string runtimemap, string wkt, bool persist)
		{
			return QueryMapFeatures(runtimemap, wkt, persist, QueryMapFeaturesLayerAttributes.Default, false);
		}

		/// <summary>
		/// Selects features from a runtime map, returning a selection Xml.
		/// </summary>
		/// <param name="runtimemap">The map to query. NOT a resourceID, only the map name!</param>
		/// <param name="wkt">The WKT of the geometry to query with (always uses intersection)</param>
		/// <param name="persist">True if the selection should be saved in the runtime map, false otherwise.</param>
		/// <param name="attributes">The type of layer to include in the query</param>
		/// <param name="raw">True if the result should contain the tooltip and link info</param>
		/// <returns>The selection Xml, or an empty string if there were no data.</returns>
        abstract public string QueryMapFeatures(string runtimemap, string wkt, bool persist, QueryMapFeaturesLayerAttributes attributes, bool raw);

        /// <summary>
        /// Renders a minature bitmap of the layers style
        /// </summary>
        /// <param name="scale">The scale for the bitmap to match</param>
        /// <param name="layerdefinition">The layer the image should represent</param>
        /// <param name="themeIndex">If the layer is themed, this gives the theme index, otherwise set to 0</param>
        /// <param name="type">The geometry type, 1 for point, 2 for line, 3 for area, 4 for composite</param>
        /// <returns>The minature bitmap</returns>
        abstract public System.Drawing.Image GetLegendImage(double scale, string layerdefinition, int themeIndex, int type);

        /// <summary>
        /// Upload a MapGuide Package file to the server
        /// </summary>
        /// <param name="filename">Name of the file to upload</param>
        /// <param name="callback">A callback argument used to display progress. May be null.</param>
        abstract public void UploadPackage(string filename, Utility.StreamCopyProgressDelegate callback);

        abstract public ResourceDocumentHeaderType GetResourceHeader(string resourceID);
        abstract public ResourceFolderHeaderType GetFolderHeader(string resourceID);
        abstract public void SetFolderHeader(string resourceID, ResourceFolderHeaderType header);
        abstract public void SetResourceHeader(string resourceID, ResourceDocumentHeaderType header);

        /// <summary>
        /// Gets a list of all users on the server
        /// </summary>
        /// <returns>The list of users</returns>
        public virtual UserList EnumerateUsers()
        {
            return this.EnumerateUsers(null);
        }

        /// <summary>
        /// Gets a list of users in a group
        /// </summary>
        /// <param name="group">The group to retrieve the users from</param>
        /// <returns>The list of users</returns>
        abstract public UserList EnumerateUsers(string group);

        /// <summary>
        /// Gets a list of all groups on the server
        /// </summary>
        /// <returns>The list of groups</returns>
        abstract public GroupList EnumerateGroups();

	}
}
