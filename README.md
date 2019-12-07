# Alexa.NET.SkillFlow.CodeGenerator
A small library that outputs Alexa.NET compatible code from a SkillFlow model

## Installation
```bash
dotnet tool install --global Alexa.NET.SkillFlow.Tool
```

## Generate Code
```bash
skillflow -i story.abc -v "skill flow"
```

## Arguments
```bash
  -i, --input         Required. The skill flow story file

  -o, --output        The directory to place the code into

  -s, --skill         The invocation name for the skill

  -r, --root          The root namespace

  -n, --nolambda      (Default: false) Produces scenes and request handlers, no lambda function

  -v, --invocation    (Default: Skill Flow) The skill invocation name

  --help              Display this help screen.

  --version           Display version information.
```
