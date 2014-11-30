using System;
using System.Data;
using Mono.Data.Sqlite;
using System.Data.Linq;

namespace BridgeChat.Core
{
    public class SystemDatastore : IDatastore
    {
        private readonly IDbConnection Connection;

        public SystemDatastore(IDbConnection connection)
        {
            Connection = connection;
        }

        public void CreateSchema()
        {
            using (var cmd = Connection.CreateCommand()) {
                cmd.CommandText =
                    "create table groups(id integer primary key, topic varchar(1023));" +
                    "create table bindings(target integer, module varchar(255), bindinfo varchar(1023), primary key (target, module), foreign key (target) references groups(id) on delete cascade);";
                cmd.ExecuteNonQuery();
            }
        }

        private void UpdateGroup(Group g)
        {
            using (var cmd = Connection.CreateCommand()) {
                cmd.CommandText = "update groups set topic=@topic where id=@groupid";
                cmd.SetParameter("groupid", g.Id);
                cmd.SetParameter("topic", g.Topic);
                cmd.ExecuteNonQuery();
            }
        }

        private void RemoveGroup(Group obj)
        {
            if (RemovalHandler != null)
                RemovalHandler(obj);

            using (var cmd = Connection.CreateCommand()) {
                cmd.CommandText = "delete from groups where id=@groupid";
                cmd.SetParameter("groupid", obj.Id);
                cmd.ExecuteNonQuery();
            }
        }

        private void CreateBinding(Binding obj)
        {
            RemoveBinding(obj); // if it exists, make sure that it's gone
            using (var cmd = Connection.CreateCommand()) {
                cmd.CommandText = "insert into bindings(target, module, bindinfo) values(@groupid, @module, @info);";
                cmd.SetParameter("groupid", obj.GroupId);
                cmd.SetParameter("module", obj.Module);
                cmd.SetParameter("info", obj.BindParams);
                cmd.ExecuteNonQuery();
            }
        }

        private void RemoveBinding(Binding obj)
        {
            using (var cmd = Connection.CreateCommand()) {
                cmd.CommandText = "delete from bindings where target=@groupid and module=@module;";
                cmd.SetParameter("groupid", obj.GroupId);
                cmd.SetParameter("module", obj.Module);
                cmd.ExecuteNonQuery();
            }
        }

        Group IDatastore.CreateGroup()
        {
            // Idea: The highest groupID + 1 is our new GroupID.
            // Let's do all that in a transaction to ensure atomicity.

            // TODO: figure out whether we can use something less strict than Serializable
            using (var transact = Connection.BeginTransaction(IsolationLevel.Serializable)) {

                Group retval;
                try {
                    uint curMaxId;
                    using (var cmd = Connection.CreateCommand()) {
                        cmd.Transaction = transact;
                        cmd.CommandText = "select max(id) from groups;";
                        var oCurMaxId = cmd.ExecuteScalar();
                        if (oCurMaxId is DBNull)
                            curMaxId = 0;
                        else
                            curMaxId = checked((uint)(long)oCurMaxId);
                    }

                    var ourId = checked(curMaxId + 1);

                    using (var cmd = Connection.CreateCommand()) {
                        cmd.Transaction = transact;
                        cmd.CommandText = "insert into groups (id) values (@groupid);";
                        cmd.SetParameter("groupid", ourId);
                        cmd.ExecuteNonQuery();
                    }

                    retval = new Group(UpdateGroup, RemoveGroup, CreateBinding, RemoveBinding, ourId);
                    transact.Commit(); // NOW everything is fine. We can commit.
                } catch (Exception) {
                    transact.Rollback();
                    throw;
                }

                return retval;
            }
        }

        public Action<Group> RemovalHandler { private get; set; }

        System.Collections.Generic.IEnumerable<Group> IDatastore.SavedGroups {
            get {
                using (var cmd = Connection.CreateCommand()) {
                    cmd.CommandText = "select id, topic from groups";
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read()) {
                            var grp = new Group(UpdateGroup, RemoveGroup, CreateBinding, RemoveBinding, checked((uint)reader.GetInt64(0)));
                            grp.SetTopic(reader.GetString(1), Module.ServerManagementModule);
                            yield return grp;
                        }
                }
            }
        }

        System.Collections.Generic.IEnumerable<Binding> IDatastore.SavedBindings {
            get {
                using (var cmd = Connection.CreateCommand()) {
                    cmd.CommandText = "select target, module, bindinfo from bindings";
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            yield return new Binding {
                                GroupId = checked((uint)reader.GetInt64(0)),
                                Module = reader.GetString(1),
                                BindParams = reader.GetString(2)
                            };
                }
            }
        }

        void IDisposable.Dispose()
        {
            Connection.Close();
            Connection.Dispose();
        }
    }
}

