using AzureTableContext.Attributes;
using AzureTableContext.Tests.Entities;
using System.Runtime.CompilerServices;

namespace AzureTableContext.Tests;

public class IntegrationTests
{
    private TableContext Configure()
    {
        var ctx = new TableContext()
            .ConfigureLocal()
            .RegisterTable<Root>()
            .RegisterTable<Base>()
            .RegisterTable<Branch>()
            .RegisterTable<Leaf>();
        return ctx;
    }

    private async Task ClearAll()
    {
        var ctx = Configure();

        var allRoot = ctx.Query<Root>("RowKey ge ''");
        await ctx.Delete(allRoot ?? [], 5);

        var allBase = ctx.Query<Base>("RowKey ge ''");
        await ctx.Delete(allBase ?? [], 5);

        var allBranch = ctx.Query<Branch>("RowKey ge ''");
        await ctx.Delete(allBranch ?? [], 5);

        var allLeaf = ctx.Query<Leaf>("RowKey ge ''");
        await ctx.Delete(allLeaf ?? [], 5);
    }

    [Fact]
    public async void TestGet_FindsOne()
    {
        await ClearAll();
        var ctx = Configure();

        var root = new Root
        {
            Id = "one",
            PartitionKey = "",
            Base = new Base
            {
                PartitionKey = "",
                Branches = [
                    new Branch { PartitionKey = "" },
                    new Branch { PartitionKey = "" },
                ]
            }
        };
        await ctx.Save(root);
        var tree = ctx.Get<Root>("one");
        Assert.NotNull(tree);
        Assert.Equal("one", tree.Id);
        Assert.NotNull(tree.Base);
        Assert.NotEmpty(tree.Base.Branches);
        Assert.Equal(2, tree.Base.Branches.Count);
    }

    [Fact]
    public async void TestGet_FindsOneOfTwo()
    {
        await ClearAll();
        var ctx = Configure();

        var root = new Root
        {
            Id = "one",
            PartitionKey = "a",
            Base = new Base
            {
                PartitionKey = "",
                Branches = [
                    new Branch { PartitionKey = "" },
                    new Branch { PartitionKey = "" },
                ]
            }
        };
        var root2 = new Root
        {
            Id = "one",
            PartitionKey = "b",
            Base = new Base
            {
                PartitionKey = "",
                Branches = [
                    new Branch { PartitionKey = "" },
                ]
            }
        };
        await ctx.Save(root, root2);
        var tree = ctx.Get<Root>("one", "a");
        Assert.NotNull(tree);
        Assert.Equal("one", tree.Id);
        Assert.NotNull(tree.Base);
        Assert.NotEmpty(tree.Base.Branches);
        Assert.Equal(2, tree.Base.Branches.Count);
    }

    [Fact]
    public async Task TestQuery_AsyncAndSyncReturnsSame()
    {
        await ClearAll();
        var ctx = Configure();

        var root1 = new Root
        {
            Id = "one",
            PartitionKey = "",
            Base = new Base
            {
                PartitionKey = "",
                Branches = [
                    new Branch { PartitionKey = "" },
                    new Branch { PartitionKey = "" },
                ]
            }
        };
        var root2 = new Root
        {
            Id = "two",
            PartitionKey = "",
            Base = new Base
            {
                PartitionKey = "",
                Branches = [
                    new Branch { PartitionKey = "" },
                    new Branch { PartitionKey = "" },
                ]
            }
        };
        var root3 = new Root
        {
            Id = "three",
            PartitionKey = "",
            Base = new Base
            {
                PartitionKey = "",
                Branches = [
                    new Branch { PartitionKey = "" },
                    new Branch { PartitionKey = "" },
                ]
            }
        };
        await ctx.Save(root1, root2, root3);
        var treeAsync = await ctx.QueryAsync<Root>("");
        var tree = ctx.Query<Root>("");

        Assert.NotNull(tree);
        Assert.NotNull(treeAsync);
        Assert.Equal(tree.First().Id, treeAsync.First().Id);
        Assert.Equal(tree.Last().Id, treeAsync.Last().Id);
        Assert.Equal(tree.Count(), treeAsync.Count());
    }

    [Fact]
    public async Task TestDelete_RemovesOne()
    {
        var root1 = new Root
        {
            Id = "one",
            PartitionKey = "",
            Base = new Base
            {
                PartitionKey = "",
                Branches = [
                    new Branch { PartitionKey = "" },
                    new Branch { PartitionKey = "" },
                ]
            }
        };
        var root2 = new Root
        {
            Id = "two",
            PartitionKey = "",
            Base = new Base
            {
                PartitionKey = "",
                Branches = [
                    new Branch { PartitionKey = "" },
                    new Branch { PartitionKey = "" },
                ]
            }
        };
        var root3 = new Root
        {
            Id = "three",
            PartitionKey = "",
            Base = new Base
            {
                PartitionKey = "",
                Branches = [
                    new Branch { PartitionKey = "" },
                    new Branch { PartitionKey = "" },
                ]
            }
        };

        var ctx = Configure();

        await ctx.Save(root1, root2, root3);

        var allTrees = (await ctx.QueryAsync<Root>("", 0))!.ToList();

        await ctx.Delete(root1, 5);

        var allTreesAfter = (await ctx.QueryAsync<Root>("", 0))!.ToList();

        Assert.NotEmpty(allTrees);
        Assert.NotEmpty(allTreesAfter);
        Assert.Equal(allTrees.Count - 1, allTreesAfter.Count);
    }

    [Fact]
    public async Task TestLambdaQuery_ShouldFindBoth()
    {
        await ClearAll();
        var ctx = Configure();

        var tree1 = new Root
        {
            Base = new() { PartitionKey = "" },
            Hello = -1,
            Id = "a",
            PartitionKey = "tree1",
        };
        var tree2 = new Root
        {
            Base = new() { PartitionKey = "" },
            Hello = 1,
            Id = "b",
            PartitionKey = "tree2",
        };
        await ctx.Save(tree1, tree2);

        var result = ctx.Query<Root>(v => v.PartitionKey == "tree2" && v.Hello > 0 || v.Id == "a");

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains(result, r => r.Id == "a");
        Assert.Contains(result, r => r.Id == "b");

        await ctx.Delete(result, 1);
    }

    [Fact]
    public async Task TestLambdaQuery_ShouldFindOne()
    {
        await ClearAll();
        var ctx = Configure();

        var tree1 = new Root
        {
            Base = new() { PartitionKey = "" },
            Hello = -1,
            Id = "a",
            PartitionKey = "tree1",
        };
        var tree2 = new Root
        {
            Base = new() { PartitionKey = "" },
            Hello = -1,
            Id = "b",
            PartitionKey = "tree2",
        };
        await ctx.Save(tree1, tree2);

        var result = ctx.Query<Root>(v => v.PartitionKey == "tree2" && v.Hello > 0 || v.Id == "a");

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains(result, r => r.Id == "a");

        await ctx.Delete(result, 1);
    }

    [Fact]
    public async Task TestLambdaQuery_ShouldFindNone()
    {
        await ClearAll();
        var ctx = Configure();

        var tree1 = new Root
        {
            Base = new() { PartitionKey = "" },
            Hello = 1,
            Id = "a",
            PartitionKey = "tree1",
        };
        var tree2 = new Root
        {
            Base = new() { PartitionKey = "" },
            Hello = -1,
            Id = "b",
            PartitionKey = "tree2",
        };
        await ctx.Save(tree1, tree2);

        var result = ctx.Query<Root>(v => v.PartitionKey == "tree2" && (v.Hello > 0 || v.Id == "a"));

        Assert.NotNull(result);
        Assert.Empty(result);
    }


    [Fact]
    public async Task TestLambdaQuery_TestCreatedDate()
    {
        await ClearAll();
        var ctx = Configure();

        var tree1 = new Root
        {
            Base = new() { PartitionKey = "" },
            Hello = 1,
            Id = "a",
            PartitionKey = "tree1",
        };
        var tree2 = new Root
        {
            Base = new() { PartitionKey = "" },
            Hello = -1,
            Id = "b",
            PartitionKey = "tree2",
        };
        await ctx.Save(tree1, tree2);

        var result = ctx.Query<Root>(v => v.CreatedAt < DateTimeOffset.UtcNow);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async void TestLambdaQuery_UsingParamValues()
    {
        await ClearAll();
        var ctx = Configure();

        var tree1 = new Root
        {
            Base = new() { PartitionKey = "" },
            Hello = 1,
            Id = "a",
            PartitionKey = "tree1",
        };
        var tree2 = new Root
        {
            Base = new() { PartitionKey = "" },
            Hello = -1,
            Id = "b",
            PartitionKey = "tree2",
        };
        await ctx.Save(tree1, tree2);

        var id = "a";
        var pk = "tree1";

        var tree = ctx.Query<Root>(r => r.Id == id && pk == r.PartitionKey)?.First();
        Assert.NotNull(tree);
        Assert.Equal("a", tree.Id);
    }

    [TableName("RankedMatches")]
    private class RankedMatch : TableModel { }
}