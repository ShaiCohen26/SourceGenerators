using System;
using System.Text.Json;

using Persisted;


namespace GeneratedDemo
{
	[Managed(EnableAudit = true, EnableSoftDelete = true)]
	public partial class ManagedEntity
	{
		[Persisted(SetOnInsert = true)]
		private int _idParent;

		[Persisted(SetOnInsert = true, SetOnUpdate = true)]
		private string _name;
	}

	public static class UseManagedEntityGenerator

	{
		public static void Run()
		{
			Guid authId = Guid.NewGuid();
			ManagedEntity entityAuthority = new ManagedEntity { Id = authId, IdParent = 1};
			ManagedEntity entityToCreate = new ManagedEntity { Id = Guid.NewGuid(), IdParent = 2, CreatedBy = "user1" };
			ManagedEntity entityToUpdate = new ManagedEntity { Id = Guid.NewGuid(), IdParent = 3, CreatedBy = "user2", ModifiedLastBy = "user2" };
			ManagedEntity entityToDelete = new ManagedEntity { Id = Guid.NewGuid(), IdParent = 4, CreatedBy = "user3", DeletedBy = "user3" };

			Console.WriteLine("\n auth entity:");
			Console.WriteLine(JsonSerializer.Serialize(entityAuthority));
			Console.WriteLine("\n auth entity after insert:");
			Console.WriteLine(JsonSerializer.Serialize(entityToCreate.MapToAuthorityInsert(entityAuthority)));
			Console.WriteLine("\n auth entity after update:");
			Console.WriteLine(JsonSerializer.Serialize(entityToUpdate.MapToAuthorityUpdate(entityAuthority)));
			Console.WriteLine("\n auth entity after delete:");
			Console.WriteLine(JsonSerializer.Serialize(entityToDelete.MapToAuthorityDelete(entityAuthority)));
		}
	}
}
