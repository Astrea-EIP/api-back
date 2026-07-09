// Runs automatically on first container start (docker-entrypoint-initdb.d convention).
// Local, disposable dev data only — never used for preprod/prod. See CONTRIBUTING.md.
//
// The domain model is still minimal (see Models/Entities). Add representative
// collections/documents here as new entities/repositories are introduced, e.g.:
//
// db = db.getSiblingDB("astrea_proto");
// db.createCollection("itineraries");
// db.itineraries.insertMany([
//   { start: "Nantes", end: "Rennes", createdAt: new Date() },
// ]);

db = db.getSiblingDB("astrea_proto");
