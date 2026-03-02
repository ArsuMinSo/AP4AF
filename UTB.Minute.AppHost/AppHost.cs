var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.UTB_Minute_DbManager>("utb-minute-dbmanager");

builder.AddProject<Projects.UTB_Minute_AdminClient>("utb-minute-adminclient");

builder.AddProject<Projects.UTB_Minute_CanteenClient>("utb-minute-canteenclient");

builder.AddProject<Projects.UTB_Minute_WebApi>("utb-minute-webapi");

builder.Build().Run();
