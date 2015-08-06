using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DataService 
{
    public class DataService<T>
    {
        private string con=ConfigurationManager.AppSettings.Get("conString");
        private IDbConnection connection;
        private string table_name=typeof(T).Name;

        public DataService(IDbConnection connection)
        {
            this.connection = connection;
        }

        public IEnumerable<T> GetAll()
        {
            using(connection)
            {
                using (IDbCommand command = connection.CreateCommand())
                {
                    connection.ConnectionString = con;
                    command.CommandText = String.Format("Select * from {0}", table_name);
                    connection.Open();
                    IDataReader reader = command.ExecuteReader();
                    return ObjectList(reader);
                 }
            }
               
        }
        public IEnumerable<T> GetAll(string query)
        {
            using (connection)
            {
                using (IDbCommand command = connection.CreateCommand())
                {
                    connection.ConnectionString = con;
                    command.CommandText = query;
                    connection.Open();
                    IDataReader reader = command.ExecuteReader();
                    return ObjectList(reader);
                }
            }

        }
        public void Add(T obj)
        {
            using (connection)
            {
                   
                    using (IDbCommand command = connection.CreateCommand())
                    {
                        connection.ConnectionString = con;
                        command.CommandText = String.Format("INSERT INTO {0} VALUES ()", table_name);
                        List<PropertyInfo> properties = typeof(T).GetProperties().ToList();

                        if (properties != null)
                        {
                            PropertyInfo lastProperty = properties.Last();
                            foreach (PropertyInfo property in properties)
                            {
                                int set = command.CommandText.Length - 1;
                                string separator = property.Equals(lastProperty) ? "" : ",";
                                IDbDataParameter parameter = command.CreateParameter();
                                parameter.ParameterName = "@" + property.Name;
                                parameter.Value = property.GetValue(obj);
                                command.CommandText = command.CommandText.Insert(set, parameter.ParameterName + separator);
                                command.Parameters.Add(parameter);
                            }
                        }
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
            }
        }
        public void Add(string query)
        {
            using (connection)
            {
                using (IDbCommand command = connection.CreateCommand())
                {
                    connection.ConnectionString = con;
                    command.CommandText = query;
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }

        }
        public void Delete(object id)
        {
            using (connection)
            {
                using (IDbCommand command = connection.CreateCommand())
                {
                    connection.ConnectionString = con;
                    PropertyInfo property = typeof(T).GetProperty("id") ?? typeof(T).GetProperty(table_name[0].ToString() + "_id");
                    string name = property.Name;
                    IDbDataParameter parameter = command.CreateParameter();
                    parameter.ParameterName = "@" + name;
                    parameter.Value = id;
                    command.CommandText = "DELETE FROM "+table_name+" WHERE "+name+"="+parameter.ParameterName;
                    command.Parameters.Add(parameter);
                    connection.Open();
                    command.ExecuteNonQuery();
                }
               
            }

        }

        public void Update(T obj,object id)
        {
            using (connection)
            {
                using (IDbCommand command = connection.CreateCommand())
                {
                    connection.ConnectionString = con;
                    PropertyInfo pr = typeof(T).GetProperty("id") ?? typeof(T).GetProperty(table_name[0].ToString() + "_id");
                    command.CommandText = "UPDATE "+table_name+" SET ";
                        List<PropertyInfo> properties = typeof(T).GetProperties().ToList();
                        if (properties != null)
                        {
                            PropertyInfo lastProperty = properties.Last();
                            foreach (PropertyInfo property in properties)
                            {
                                int set = command.CommandText.Length;
                                string separator = property.Equals(lastProperty) ? "" : ",";
                                if (!(table_name[0].ToString() + "_id").Equals(property.Name) && !property.Name.Equals("id"))
                                {
                                    IDbDataParameter parameter = command.CreateParameter();
                                    parameter.ParameterName = "@" + property.Name;
                                    parameter.Value = property.GetValue(obj);
                                    command.CommandText = command.CommandText.Insert(set, property.Name + "=" + parameter.ParameterName + separator);
                                    command.Parameters.Add(parameter);
                                }


                            }
                            IDbDataParameter idParameter = command.CreateParameter();
                            idParameter.ParameterName = "@" + pr.Name;
                            idParameter.Value = id;
                            command.CommandText = command.CommandText.Insert(command.CommandText.Length, " WHERE "+pr.Name+"="+idParameter.ParameterName);
                            command.Parameters.Add(idParameter);
                        }

                        connection.Open();
                        command.ExecuteNonQuery();
                }
            }
        }
        public void Update(string query)
        {
            using (connection)
            {
               
                    using( IDbCommand command = connection.CreateCommand())
                    {
                        connection.ConnectionString = con;
                        command.CommandText = String.Format("UPDATE {0} SET ",table_name);
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
              
            }
        }

        private List<T>ObjectList(IDataReader reader)
        {
            List<T> objectlist = new List<T>();
            List<PropertyInfo> properties = typeof(T).GetProperties().ToList(); 
            if (properties != null)
            {
                while (reader.Read())
                {
                    var item = Activator.CreateInstance<T>();
                    foreach (PropertyInfo property in properties)
                    {
                        if (!reader.IsDBNull(reader.GetOrdinal(property.Name)))
                        {
                            Type convertTo = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                            var value=Convert.ChangeType(reader[property.Name], convertTo);
                            property.SetValue(item, value , null);
                        }
                    }
                    objectlist.Add(item);
                }
                return objectlist;
            }
            return null;
        }
       
    }
}
