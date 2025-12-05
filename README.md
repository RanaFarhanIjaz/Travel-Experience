# 🌍 TravelShare - Travel Experience Sharing Platform
🚀 Step-by-Step Installation
📥 Step 1: Clone the Repository
bash
# Clone the project
git clone https://github.com/RanaFarhanIjaz/Travel-Experience.git

# Navigate to project
cd Travel-Experience
📦 Step 2: Install Dependencies
Automatic Installation (Recommended):
Run this command to install all required packages:

bash
# This installs ALL required packages:
dotnet add package Microsoft.EntityFrameworkCore.Sqlite --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.0
dotnet add package Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation --version 8.0.0
dotnet add package Microsoft.AspNetCore.Session --version 2.2.0
dotnet add package Microsoft.Extensions.Http --version 8.0.0
dotnet add package System.Text.Json --version 8.0.0
dotnet add package Microsoft.Extensions.Logging --version 8.0.0
dotnet add package Microsoft.Extensions.Logging.Console --version 8.0.0
dotnet add package Microsoft.Extensions.Configuration --version 8.0.0
dotnet add package Microsoft.Extensions.Configuration.Json --version 8.0.0
Or restore packages from .csproj:
bash
# This reads from TravelShare.csproj and installs all packages
dotnet restore
📋 Step 3: Verify Dependencies
Check if all packages are installed:

bash
dotnet list package
You should see these packages:

text
Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation   8.0.0
Microsoft.AspNetCore.Session                        2.2.0
Microsoft.EntityFrameworkCore.Sqlite                8.0.0
Microsoft.EntityFrameworkCore.Design                8.0.0
Microsoft.Extensions.Http                           8.0.0
System.Text.Json                                    8.0.0
Microsoft.Extensions.Logging                        8.0.0
Microsoft.Extensions.Logging.Console                8.0.0
🔑 Step 4: Get Groq API Key
Go to console.groq.com

Sign up (FREE, no credit card needed)

Click "Create API Key"

Copy your key (looks like gsk_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx)

⚙️ Step 5: Configure Application
Create appsettings.json file in project root:

json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=travelshare.db"
  },
  "Groq": {
    "ApiKey": "YOUR_GROQ_API_KEY_HERE",
    "Model": "llama3-70b-8192"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
Replace YOUR_GROQ_API_KEY_HERE with your actual key.

🔨 Step 6: Build and Run
bash
# Build the project
dotnet build

# Run the application
dotnet run
🌐 Step 7: Access Application
Open browser and go to:

http://localhost:5000

or https://localhost:5001

🔍 Dependency Details
Core Dependencies & Their Purpose:
Package	Purpose	Required For
Microsoft.EntityFrameworkCore.Sqlite	Database operations	Storing reviews, users
Microsoft.EntityFrameworkCore.Design	Database migrations	Creating/updating database
Microsoft.AspNetCore.Mvc.Razor	Web pages rendering	All views/UI
Microsoft.AspNetCore.Session	User sessions	Login/logout functionality
Microsoft.Extensions.Http	HTTP requests	Groq API calls
System.Text.Json	JSON processing	Parsing API responses
Microsoft.Extensions.Logging	Application logging	Debugging & monitoring
Installation Scripts:
For Windows (PowerShell):

powershell
# Save as install-dependencies.ps1
$packages = @(
    "Microsoft.EntityFrameworkCore.Sqlite",
    "Microsoft.EntityFrameworkCore.Design",
    "Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation",
    "Microsoft.AspNetCore.Session",
    "Microsoft.Extensions.Http",
    "System.Text.Json",
    "Microsoft.Extensions.Logging",
    "Microsoft.Extensions.Logging.Console"
)

foreach ($package in $packages) {
    Write-Host "Installing $package..." -ForegroundColor Cyan
    dotnet add package $package --version 8.0.0
}
For Mac/Linux (Bash):

bash
# Save as install-dependencies.sh
#!/bin/bash
echo "Installing TravelShare dependencies..."

packages=(
    "Microsoft.EntityFrameworkCore.Sqlite"
    "Microsoft.EntityFrameworkCore.Design"
    "Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation"
    "Microsoft.AspNetCore.Session"
    "Microsoft.Extensions.Http"
    "System.Text.Json"
    "Microsoft.Extensions.Logging"
    "Microsoft.Extensions.Logging.Console"
)

for package in "${packages[@]}"; do
    echo "📦 Installing $package..."
    dotnet add package $package --version 8.0.0
done

echo "✅ All dependencies installed!"
🛠️ Troubleshooting Dependencies
Error: "Package not found"
bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore packages
dotnet restore
Error: "Version conflict"
bash
# List all packages and versions
dotnet list package

# Remove conflicting package
dotnet remove package PackageName

# Reinstall
dotnet add package PackageName --version 8.0.0
Error: "CS0246: The type or namespace name could not be found"
This means a package is missing. Install it:

bash
# Common missing packages:
dotnet add package Microsoft.Extensions.Http
dotnet add package System.Text.Json
dotnet add package Microsoft.Extensions.Logging
Check .NET SDK Version:
bash
dotnet --version
# Should show 8.0.x
📁 Project Structure
text
TravelShare/
├── TravelShare.csproj          # Dependencies file
├── Program.cs                  # Application entry point
├── appsettings.json           # Configuration (create this)
├── appsettings.example.json   # Template (provided)
├── Models/
│   ├── Review.cs              # Review model
│   └── User.cs               # User model
├── Data/
│   └── ApplicationDbContext.cs # Database context
├── Services/
│   └── GroqService.cs         # AI service
├── Controllers/
│   ├── HomeController.cs
│   ├── ReviewController.cs
│   └── AIController.cs
└── Views/                     # All web pages
🧪 Verify Installation
Test 1: Build Success
bash
dotnet build
# Should show: "Build succeeded"
Test 2: Database Creation
bash
# Run the app
dotnet run

# Check if database file is created
ls travelshare.db
# Should show the database file
Test 3: AI Connection
Start app: dotnet run

Visit: http://localhost:5000/testgroq

Should show: "Groq Test Response: Yes I am working!"

🔄 Update Dependencies
To update all packages to latest versions:

bash
# List outdated packages
dotnet list package --outdated

# Update all packages
dotnet update
📦 Package Management Commands
Command	Purpose
dotnet add package <name>	Install new package
dotnet remove package <name>	Remove package
dotnet list package	List installed packages
dotnet restore	Restore all packages
dotnet nuget locals all --clear	Clear package cache
🚨 Common Issues & Solutions
Issue: "UseSqlite not found"
Solution:

bash
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
Issue: "HttpClient not found"
Solution:

bash
dotnet add package Microsoft.Extensions.Http
Issue: "ILogger not found"
Solution:

bash
dotnet add package Microsoft.Extensions.Logging
Issue: "JsonDocument not found"
Solution:

bash
dotnet add package System.Text.Json
✅ Final Checklist
Before running the app, ensure:

.NET 8.0 SDK installed (dotnet --version)

All packages installed (dotnet list package)

appsettings.json with Groq API key

Database can be created

Build succeeds (dotnet build)

