## ADR-002: Seeded Sign In - 20-10-2026

## Status
Accepted - 2026-10-20

## Context
The decision for this is to implement the seeded user approach. This is due to the scope and time frame given for this assessment. In this scenario, the users will be provided to us of who can access the system and which users will be provided to sign in on the frontend. For the sign in, it will use JWT based authentication for the users to validate their sign on validity. 

# Options Considered
* Session Tokens
* Signed Tokens using JWT

## Decision
I went with the approach of using the JWT signed tokens. This is due to the scalability and security that comes with the JWT auth tokens. It will also have a way of expiring the token within the application since it is in the token


## Consequences
**Positive**

- This provides a safe way for data to be transmitted from the frontend to our backend. This way we can also trust the data that is coming has it will be signed with the token. The user claims will also be a part of the token

**Negative**
- We lose the control of negating the token since it will be valid once the token has been issued, even if there is a compromised users account
