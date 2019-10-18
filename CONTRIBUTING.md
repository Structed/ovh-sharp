This file contains guidelines that should be followed when making any
changes to the repository. All text before the first level 1 (L1) header
will be ignored by the Pull request guidelines add-on.

# Process
1. Reviewer Checks out code
2. Reviewer builds & does a developer test
3. Reviewer checks if ReleaseNotes.md has been updated
3. Reviewer reviews actual Code
4. Reviewer adds comments & tasks
5. Implementer makes changes, if required
6. Reviewer repeats steps 1-4
7. Reviewer merges


# Coding Guidelines
We follow the following <a href="https://re-flekt.atlassian.net/wiki/display/TELE/Coding+Guidelines" target="_blank">Coding Guidelines & Decisions</a>

Here is a Breakdown:
* StyleCop rules are to be followed
* We do not use abbreviations, if possible
* Test classes are suffixed with the word "Tests" (plural)
* Test method are named after the following schema:
 `MethodName_StateUnderTest_ExpectedBehavior`
* We treat warnings as errors. No new Warnings allowed!

# Decline Rules
* Project is not building
* Unit tests fail
* StyleCop warnings appear