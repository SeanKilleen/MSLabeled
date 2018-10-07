# MSLabeled
A .net core project to get all of the Microsoft repositories with up-for-grabs style tags &amp; output information so I can add them to up-for-grabs.net. Double as a sample project for .NET Core 2.1 + Octokit. Also uses a touch of Serilog/Seq for ease of use.

## Prior to Running

### Prerequisites: 

* .NET Core v2.1.x
* [Seq](http://getseq.net) if you'd like to see the logging output.

### Setting the User Secrets

This project makes use of user secrets, a great out-of-the-box feature of .NET core v2.1.

To set them, do the following:

* Create a GitHub personal access token so you'll have information to provide.
* Open a console and navigate to `src\mslabeled` within this app
* Run the following, substituting your information: 

```
dotnet user-secrets set GitHubUser "USERNAME"
dotnet user-secrets set GitHubToken "TOKEN"`
```

## How to build & run

* Pull this repository
* Open the `/src` directory in a terminal
* `dotnet restore`
* `dotnet build`
* `dotnet run`
* Press any key to start
* Check out your Seq instance at <http://localhost:5341> to see the output.

## How did this come about?

Microsoft jumped into the [Hacktoberfest](https://hacktoberfest.digitalocean.com/) to offer a t-shirt for any PRs against their repositories, and I thought that was pretty cool. I'm also a fan of [up-for-grabs.net](http://up-for-grabs.net). So I figured I'd use this project as a little way to contribute to OSS myself while also benefiting both Microsoft devs and up-for-grabs.

More on the process in an upcoming blog post! Will add the link here when it's done.