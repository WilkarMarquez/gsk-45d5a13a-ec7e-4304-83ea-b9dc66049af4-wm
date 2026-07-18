CREATE TABLE "Users"
(
    "Id" UUID PRIMARY KEY,

    "ExternalId" VARCHAR(100) NOT NULL,

    "Name" VARCHAR(150) NOT NULL,

    "CreatedAt" TIMESTAMP NOT NULL
);

CREATE UNIQUE INDEX "UX_Users_ExternalId"
ON "Users"("ExternalId");




CREATE TABLE "ScoreEvents"
(
    "Id" UUID PRIMARY KEY,

    "UserId" UUID NOT NULL,

    "Score" INTEGER NOT NULL,

    "EventTimestamp" TIMESTAMP NOT NULL,

    "CreatedAt" TIMESTAMP NOT NULL,

    CONSTRAINT "FK_ScoreEvents_Users"
        FOREIGN KEY ("UserId")
        REFERENCES "Users"("Id")
);

CREATE INDEX "IX_ScoreEvents_UserId"
ON "ScoreEvents"("UserId");

CREATE INDEX "IX_ScoreEvents_EventTimestamp"
ON "ScoreEvents"("EventTimestamp");

CREATE INDEX "IX_ScoreEvents_UserId_EventTimestamp"
ON "ScoreEvents"("UserId","EventTimestamp");





CREATE TABLE "UserAggregates"
(
    "UserId" UUID PRIMARY KEY,

    "TotalScore" BIGINT NOT NULL,

    "LastUpdated" TIMESTAMP NOT NULL,

    CONSTRAINT "FK_UserAggregates_Users"
        FOREIGN KEY ("UserId")
        REFERENCES "Users"("Id")
);

CREATE INDEX "IX_UserAggregates_TotalScore"
ON "UserAggregates"("TotalScore" DESC);