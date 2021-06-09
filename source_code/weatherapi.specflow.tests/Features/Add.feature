Feature: Add

Scenario: Add two numbers
	When i add 2 and 3
	Then the result should be 5

Scenario: Add two zeros
	When i add 0 and 0
	Then the result should be 0

Scenario: Add one negative and one positive
	When i add -1 and 3
	Then the result should be 2
