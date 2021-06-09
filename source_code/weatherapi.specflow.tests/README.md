# Add BDD specflow test

-   run the follwing command

```bash
mkdir weatherapi.specflow.tests
cd weatherapi.specflow.tests

dotnet new unit
dotnet add package "SpecFlow.NUnit"
dotnet add package "SpecFlow.Plus.LivingDocPlugin"

mkdir Drivers
mkdir Features
mkdir Hooks
mkdir Steps
rm UnitTest1.cs

touch Steps/AdditionStepDefinition.cs
touch Features/Add.feature
```

-   Add the folowing to the AdditionStepDefinition.cs file

```csharp
using NUnit.Framework;
using TechTalk.SpecFlow;

namespace SpecFlowDemo.Steps
{
    [Binding]
    public sealed class AdditionStepDefinitions
    {
        private readonly ScenarioContext _scenarioContext;
        private int num1 { get; set; }
        private int num2 { get; set; }

        public AdditionStepDefinitions(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
        }

        [When("i add (.*) and (.*)")]
        public void WhenTheTwoNumbersAreAdded(int number1, int number2)
        {
            num1 = number1;
            num2 = number2;
        }

        [Then("the result should be (.*)")]
        public void ThenTheResultShouldBe(int result)
        {
            Assert.AreEqual((num1 + num2), result);
        }
    }
}
```


- add the following to the Add.feature file

```
Feature: Add

Scenario: Add two numbers
	When i add 2 and 3
	Then the result should be 5

```

- run `dotnet test`
- notice all tests are passed and you can see generated file under the `/Features` folder