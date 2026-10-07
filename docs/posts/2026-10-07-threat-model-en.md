# LinkedIn post draft — October 7, 2026

Before looking for vulnerabilities in my API, I had to answer a simpler question: **what is it supposed to protect?**

I chose a small example for my application security lab: private notes. A client may request a note by ID, but that ID is only a request. Even if the person is signed in, the API still needs to decide whether they can read or change *that specific note*.

I mapped three actors: the owner, another signed-in user, and an anonymous user. Then I marked the trust boundary: the client controls the requested ID and the content it sends; the server must make the access decision using the note's owner.

That gave me three hypotheses to test locally: unauthorized reads, unauthorized changes, and creating a note with an owner chosen by the client.

I haven't reproduced a vulnerability yet. The local API now keeps its private-note routes closed, and seven baseline tests verify that a client-supplied identity does not open them. For now, the key lesson from the model is simple: **authentication tells the API who is calling; authorization decides what that person can do with a specific resource.**

Next: turn these hypotheses into local tests with fictional users as the API grows.

---

**Editorial note (not part of the post):** This describes a design analysis, not a completed security test. Add a repository link only after checking the remote and published content.

**Companion image:** [authentication-vs-authorization-linkedin.png](assets/authentication-vs-authorization-linkedin.png)

**Suggested LinkedIn alt text:** Diagram of a person requesting a private note. The request passes through authentication, asking “Who is calling?”, and authorization, asking “Can they access this note?”. The headline says a valid login does not grant access to every note.
