namespace Initialization

open Orleans
open Orleans.Hosting
open Orleans.TestingHost
open global.ShoppingApp.Abstractions
open global.ShoppingApp.Grains
open System
open Xunit

module CurrentAssembly =
    [<Literal>]
    let ClusterFixture = "ClusterFixture"

type TestSiloConfigurator() =
    interface ISiloConfigurator with 
        member this.Configure(siloBuilder: ISiloBuilder) =
                siloBuilder.AddMemoryGrainStorage(PersistentStorageConfig.AzureSqlName) |> ignore
                siloBuilder.AddMemoryGrainStorage(PersistentStorageConfig.AzureStorageName) |> ignore 

type ClusterFixture() =
    let mutable cluster = null

    do
        let builder = new TestClusterBuilder()
        builder.AddSiloBuilderConfigurator<TestSiloConfigurator>() |> ignore
        cluster <- builder.Build()
        cluster.Deploy()

    member this.Cluster with get() = cluster
    interface IDisposable with member this.Dispose() = cluster.Dispose()

[<CollectionDefinition(CurrentAssembly.ClusterFixture)>]
type ClusterCollection() = 
    interface ICollectionFixture<ClusterFixture> with
