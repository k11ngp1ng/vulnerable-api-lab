# LinkedIn post draft — Day 2: when input becomes SQL

What makes a text field change the meaning of a database query?

I tested that question in my local ASP.NET Core and PostgreSQL lab using two fictional notes.

One test-only query placed a supplied title directly into the SQL string. A crafted title changed the `WHERE` condition, and the query returned **2 note IDs** instead of the **0** that an exact title match should return.

The corrected query kept the SQL fixed and sent the title as a parameter. The same input returned **0 IDs**. A normal title still returned its one expected note in both versions.

The cause was the trust boundary: the concatenated value became part of the instruction the database parsed. With a parameter, that value remained data for the comparison.

This is a deliberately narrow result. The unsafe query exists only in the test project, with fictional records in a local database. No vulnerable HTTP endpoint was added, and the private-note routes remain closed while authentication and ownership checks are unfinished. Two integration tests cover this comparison; the full suite passed with 9 tests when the local database was available.

My takeaway: **validate input for your rules, but parameterize values so input never has to be interpreted as SQL syntax.**

#AppSec #SQLInjection #ASPNetCore #PostgreSQL

---

**Editorial note (not part of the post):** Draft for author review. The observed result is 2 versus 0 note IDs for one input in a local read-only test; it does not show HTTP data exposure or prove all queries safe. No repository link is included until public visibility is confirmed. Publishing remains the author's decision.

**Companion image:** [day2-input-as-data.png](assets/day2-input-as-data.png)

**Suggested LinkedIn alt text:** A two-column diagram compares a concatenated SQL query with a parameterized query. On the left, a title input merges into the SQL instruction and the result reads “2 IDs.” On the right, the input remains separate as data and the result reads “0 IDs.” The headline says “WHEN INPUT BECOMES SQL.” A footer identifies a local Day 2 lab.
