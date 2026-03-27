// Copyright Finbuckle LLC, Andrew White, and Contributors.
// Refer to the solution LICENSE file for more information.

using System.Data.Common;
using Finbuckle.MultiTenant.Vault.Abstractions;
using Finbuckle.MultiTenant.Vault.EntityFrameworkCore.Extensions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Finbuckle.MultiTenant.Vault.EntityFrameworkCore.Test.Extensions.MultiTenantDbContextExtensions;

public class MultiTenantDbContextExtensionsShould
{
    private readonly DbContextOptions _options;
    private readonly DbConnection _connection;

    private readonly Guid _abc = Guid.Parse("c8d1f3c7-440e-4e76-bc77-12e33f39136e");
    private readonly Guid _mismatch = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    
    public MultiTenantDbContextExtensionsShould()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _options = new DbContextOptionsBuilder()
            .UseSqlite(_connection)
            .Options;
    }

    [Fact]
    public void HandleTenantNotSetWhenAttaching()
    {
        try
        {
            _connection.Open();
            var tenant1 = new TenantInfo { Id = _abc, Identifier = "abc", Name = "abc" };

            // TenantNotSetMode.Throw, should act as Overwrite when adding
            using (var db = new TestDbContext(tenant1, _options))
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
                db.EnforceMultiTenantOnTracking();
                db.TenantNotSetMode = TenantNotSetMode.Throw;

                var blog1 = new Blog { Title = "abc" };
                db.Blogs?.Add(blog1);
                Assert.Equal(tenant1.Id, db.Entry(blog1).Property("VaultId").CurrentValue);
            }

            // TenantNotSetMode.Overwrite
            using (var db = new TestDbContext(tenant1, _options))
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

                db.TenantNotSetMode = TenantNotSetMode.Overwrite;
                db.EnforceMultiTenantOnTracking();
                var blog1 = new Blog { Title = "abc2" };
                db.Blogs?.Add(blog1);
                Assert.Equal(tenant1.Id, db.Entry(blog1).Property("VaultId").CurrentValue);
            }
        }
        finally
        {
            _connection.Close();
        }
    }

    [Fact]
    public void HandleTenantNotSetWhenAdding()
    {
        try
        {
            _connection.Open();
            var tenant1 = new TenantInfo { Id = _abc, Identifier = "abc", Name = "abc" };

            // TenantNotSetMode.Throw, should act as Overwrite when adding
            using (var db = new TestDbContext(tenant1, _options))
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
                db.TenantNotSetMode = TenantNotSetMode.Throw;

                var blog1 = new Blog { Title = "abc" };
                db.Blogs?.Add(blog1);
                db.SaveChanges();
                Assert.Equal(tenant1.Id, db.Entry(blog1).Property("VaultId").CurrentValue);
            }

            // TenantNotSetMode.Overwrite
            using (var db = new TestDbContext(tenant1, _options))
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
                db.TenantNotSetMode = TenantNotSetMode.Overwrite;

                var blog1 = new Blog { Title = "abc2" };
                db.Blogs?.Add(blog1);
                db.SaveChanges();
                Assert.Equal(tenant1.Id, db.Entry(blog1).Property("VaultId").CurrentValue);
            }
        }
        finally
        {
            _connection.Close();
        }
    }

    [Fact]
    public void HandleTenantMismatchWhenAdding()
    {
        try
        {
            _connection.Open();
            var tenant1 = new TenantInfo { Id = _abc, Identifier = "abc", Name = "abc" };

            // TenantMismatchMode.Throw
            using (var db = new TestDbContext(tenant1, _options))
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
                db.TenantMismatchMode = TenantMismatchMode.Throw;

                var blog1 = new Blog { Title = "abc" };
                db.Blogs?.Add(blog1);
                db.Entry(blog1).Property("VaultId").CurrentValue = _mismatch;

                Assert.Throws<MultiTenantException>(() => db.SaveChanges());
            }

            // TenantMismatchMode.Ignore
            using (var db = new TestDbContext(tenant1, _options))
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
                db.TenantMismatchMode = TenantMismatchMode.Ignore;

                var blog1 = new Blog { Title = "34" };
                db.Blogs?.Add(blog1);
                db.Entry(blog1).Property("VaultId").CurrentValue = _mismatch;
                db.SaveChanges();
                Assert.Equal(_mismatch, db.Entry(blog1).Property("VaultId").CurrentValue);
            }

            // TenantMismatchMode.Overwrite
            using (var db = new TestDbContext(tenant1, _options))
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
                db.TenantMismatchMode = TenantMismatchMode.Overwrite;

                var blog1 = new Blog { Title = "77" };
                db.Blogs?.Add(blog1);
                db.Entry(blog1).Property("VaultId").CurrentValue = _mismatch;
                db.SaveChanges();
                Assert.Equal(tenant1.Id, db.Entry(blog1).Property("VaultId").CurrentValue);
            }
        }
        finally
        {
            _connection.Close();
        }
    }

    [Fact]
    public void HandleTenantNotSetWhenUpdating()
    {
        try
        {
            _connection.Open();
            var tenant1 = new TenantInfo { Id = _abc, Identifier = "abc", Name = "abc" };

            // TenantNotSetMode.Throw
            using (var db = new TestDbContext(tenant1, _options))
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

                var blog1 = new Blog { Title = "abc" };
                db.Blogs?.Add(blog1);
                db.SaveChanges();

                db.TenantNotSetMode = TenantNotSetMode.Throw;
                db.Entry(blog1).Property("VaultId").CurrentValue = Guid.Empty;

                Assert.Throws<MultiTenantException>(() => db.SaveChanges());
            }

            // TenantNotSetMode.Overwrite
            using (var db = new TestDbContext(tenant1, _options))
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

                var blog1 = new Blog { Title = "abc12" };
                db.Blogs?.Add(blog1);
                db.SaveChanges();

                db.TenantNotSetMode = TenantNotSetMode.Overwrite;
                db.Entry(blog1).Property("VaultId").CurrentValue = Guid.Empty;
                db.SaveChanges();

                Assert.Equal(tenant1.Id, db.Entry(blog1).Property("VaultId").CurrentValue);
            }
        }
        finally
        {
            _connection.Close();
        }
    }

    [Fact]
    public void HandleTenantMismatchWhenUpdating()
    {
        try
        {
            _connection.Open();
            var tenant1 = new TenantInfo { Id = _abc, Identifier = "abc", Name = "abc" };

            // TenantMismatchMode.Throw
            using (var db = new TestDbContext(tenant1, _options))
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

                var blog1 = new Blog { Title = "abc" };
                db.Blogs?.Add(blog1);
                db.SaveChanges();

                db.TenantMismatchMode = TenantMismatchMode.Throw;
                db.Entry(blog1).Property("VaultId").CurrentValue = _mismatch;

                Assert.Throws<MultiTenantException>(() => db.SaveChanges());
            }

            // TenantMismatchMode.Ignore
            using (var db = new TestDbContext(tenant1, _options))
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

                var blog1 = new Blog { Title = "abc" };
                db.Blogs?.Add(blog1);
                db.SaveChanges();

                db.TenantMismatchMode = TenantMismatchMode.Ignore;
                db.Entry(blog1).Property("VaultId").CurrentValue = _mismatch;
                db.SaveChanges();

                Assert.Equal(_mismatch, db.Entry(blog1).Property("VaultId").CurrentValue);
            }

            // TenantMismatchMode.Overwrite
            using (var db = new TestDbContext(tenant1, _options))
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

                var blog1 = new Blog { Title = "abc12" };
                db.Blogs?.Add(blog1);
                db.SaveChanges();

                db.TenantMismatchMode = TenantMismatchMode.Overwrite;
                db.Entry(blog1).Property("VaultId").CurrentValue = _mismatch;
                db.SaveChanges();

                Assert.Equal(tenant1.Id, db.Entry(blog1).Property("VaultId").CurrentValue);
            }
        }
        finally
        {
            _connection.Close();
        }
    }

    [Fact]
    public void HandleTenantNotSetWhenDeleting()
    {
        try
        {
            _connection.Open();
            var tenant1 = new TenantInfo { Id = _abc, Identifier = "abc", Name = "abc" };

            // TenantNotSetMode.Throw
            using (var db = new TestDbContext(tenant1, _options))
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

                var blog1 = new Blog { Title = "abc" };
                db.Blogs?.Add(blog1);
                db.SaveChanges();

                db.TenantNotSetMode = TenantNotSetMode.Throw;
                db.Entry(blog1).Property("VaultId").CurrentValue = Guid.Empty;
                db.Blogs?.Remove(blog1);

                Assert.Throws<MultiTenantException>(() => db.SaveChanges());
            }

            // TenantNotSetMode.Overwrite
            using (var db = new TestDbContext(tenant1, _options))
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

                var blog1 = new Blog { Title = "abc" };
                db.Blogs?.Add(blog1);
                db.SaveChanges();

                db.TenantNotSetMode = TenantNotSetMode.Overwrite;
                db.Entry(blog1).Property("VaultId").CurrentValue = Guid.Empty;
                db.Blogs?.Remove(blog1);

                Assert.Equal(1, db.SaveChanges());
            }
        }
        finally
        {
            _connection.Close();
        }
    }

    [Fact]
    public void HandleTenantMismatchWhenDeleting()
    {
        try
        {
            _connection.Open();
            var tenant1 = new TenantInfo { Id = _abc, Identifier = "abc", Name = "abc" };

            // TenantMismatchMode.Throw
            using (var db = new TestDbContext(tenant1, _options))
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

                var blog1 = new Blog { Title = "abc" };
                db.Blogs?.Add(blog1);
                db.SaveChanges();

                db.TenantMismatchMode = TenantMismatchMode.Throw;
                db.Entry(blog1).Property("VaultId").CurrentValue = _mismatch;
                db.Blogs?.Remove(blog1);

                Assert.Throws<MultiTenantException>(() => db.SaveChanges());
            }

            // TenantMismatchMode.Ignore
            using (var db = new TestDbContext(tenant1, _options))
            {
                db.TenantMismatchMode = TenantMismatchMode.Ignore;
                var blog1 = db.Blogs?.First();
                db.Entry(blog1!).Property("VaultId").CurrentValue = _mismatch;
                db.Blogs?.Remove(blog1!);

                Assert.Equal(1, db.SaveChanges());
            }

            // TenantMismatchMode.Overwrite
            using (var db = new TestDbContext(tenant1, _options))
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

                var blog1 = new Blog { Title = "abc" };
                db.Blogs?.Add(blog1);
                db.SaveChanges();

                db.TenantMismatchMode = TenantMismatchMode.Overwrite;
                db.Entry(blog1).Property("VaultId").CurrentValue = _mismatch;
                db.Blogs?.Remove(blog1);

                Assert.Equal(1, db.SaveChanges());
            }
        }
        finally
        {
            _connection.Close();
        }
    }
}
