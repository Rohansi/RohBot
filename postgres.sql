-- Copypasted from pgAdmin
-- Make sure you create server_group and server roles first.

-- Database: rohbot
CREATE DATABASE rohbot
  WITH OWNER = server_group
       ENCODING = 'UTF8'
       TABLESPACE = pg_default
       LC_COLLATE = 'en_US.UTF-8'
       LC_CTYPE = 'en_US.UTF-8'
       CONNECTION LIMIT = -1;
GRANT ALL ON DATABASE rohbot TO server_group;
REVOKE ALL ON DATABASE rohbot FROM public;

-- Schema: rohbot
CREATE SCHEMA rohbot
  AUTHORIZATION server_group;

GRANT ALL ON SCHEMA rohbot TO server_group;
COMMENT ON SCHEMA rohbot
  IS 'standard public schema';

-- Table: rohbot.accounts
CREATE TABLE rohbot.accounts
(
  id bigserial NOT NULL,
  address text NOT NULL,
  name text NOT NULL,
  password text NOT NULL,
  salt text NOT NULL,
  enabledstyle text NOT NULL DEFAULT ''::text,
  rooms text[] NOT NULL DEFAULT '{}'::text[],
  CONSTRAINT accounts_pkey PRIMARY KEY (id)
)
WITH (
  OIDS=FALSE
);
ALTER TABLE rohbot.accounts
  OWNER TO server_group;
GRANT ALL ON TABLE rohbot.accounts TO server_group;

-- Index: rohbot.accounts_lower_idx
CREATE UNIQUE INDEX accounts_lower_idx
  ON rohbot.accounts
  USING btree
  (lower(name) COLLATE pg_catalog."default");

-- Index: rohbot.address_idx
CREATE INDEX address_idx
  ON rohbot.accounts
  USING btree
  (address COLLATE pg_catalog."default");

-- Table: rohbot.chathistory
CREATE TABLE rohbot.chathistory
(
  id bigserial NOT NULL,
  type text NOT NULL,
  date bigint NOT NULL,
  chat text NOT NULL,
  content text NOT NULL,
  usertype text, -- ChatLine
  sender text, -- ChatLine
  senderid text, -- ChatLine
  senderstyle text, -- ChatLine
  ingame boolean, -- ChatLine
  state text, -- StateLine
  "for" text, -- StateLine
  forid text, -- StateLine
  by text, -- StateLine
  byid text, -- StateLine
  fortype text, -- StateLine
  bytype text, -- StateLine
  forstyle text, -- StateLine
  bystyle text, -- StateLine
  CONSTRAINT chathistory_pkey PRIMARY KEY (id)
)
WITH (
  OIDS=FALSE
);
ALTER TABLE rohbot.chathistory
  OWNER TO server_group;
GRANT ALL ON TABLE rohbot.chathistory TO server_group;
COMMENT ON COLUMN rohbot.chathistory.usertype IS 'ChatLine';
COMMENT ON COLUMN rohbot.chathistory.sender IS 'ChatLine';
COMMENT ON COLUMN rohbot.chathistory.senderid IS 'ChatLine';
COMMENT ON COLUMN rohbot.chathistory.senderstyle IS 'ChatLine';
COMMENT ON COLUMN rohbot.chathistory.ingame IS 'ChatLine';
COMMENT ON COLUMN rohbot.chathistory.state IS 'StateLine';
COMMENT ON COLUMN rohbot.chathistory."for" IS 'StateLine';
COMMENT ON COLUMN rohbot.chathistory.forid IS 'StateLine';
COMMENT ON COLUMN rohbot.chathistory.by IS 'StateLine';
COMMENT ON COLUMN rohbot.chathistory.byid IS 'StateLine';
COMMENT ON COLUMN rohbot.chathistory.fortype IS 'StateLine';
COMMENT ON COLUMN rohbot.chathistory.bytype IS 'StateLine';
COMMENT ON COLUMN rohbot.chathistory.forstyle IS 'StateLine';
COMMENT ON COLUMN rohbot.chathistory.bystyle IS 'StateLine';

-- Index: rohbot.chathistory_chat_date_idx
CREATE INDEX chathistory_chat_date_idx
  ON rohbot.chathistory
  USING btree
  (chat COLLATE pg_catalog."default", date DESC);

-- Table: rohbot.logintokens
CREATE TABLE rohbot.logintokens
(
  id bigserial NOT NULL,
  name text NOT NULL,
  address text NOT NULL,
  token text NOT NULL,
  created bigint NOT NULL,
  CONSTRAINT logintokens_pkey PRIMARY KEY (id)
)
WITH (
  OIDS=FALSE
);
ALTER TABLE rohbot.logintokens
  OWNER TO server_group;
GRANT ALL ON TABLE rohbot.logintokens TO server_group;

-- Index: rohbot.logintokens_created_idx
CREATE INDEX logintokens_created_idx
  ON rohbot.logintokens
  USING btree
  (created DESC);

-- Index: rohbot.logintokens_lower_idx
CREATE INDEX logintokens_lower_idx
  ON rohbot.logintokens
  USING btree
  (lower(name) COLLATE pg_catalog."default");

-- Table: rohbot.roomsettings
CREATE TABLE rohbot.roomsettings
(
  id bigserial NOT NULL,
  room text NOT NULL,
  bans text[] NOT NULL DEFAULT '{}'::text[],
  mods text[] NOT NULL DEFAULT '{}'::text[],
  CONSTRAINT roomsettings_pkey PRIMARY KEY (id)
)
WITH (
  OIDS=FALSE
);
ALTER TABLE rohbot.roomsettings
  OWNER TO server_group;
GRANT ALL ON TABLE rohbot.roomsettings TO server_group;

-- Index: rohbot.roomsettings_lower_idx
CREATE INDEX roomsettings_lower_idx
  ON rohbot.roomsettings
  USING btree
  (lower(room) COLLATE pg_catalog."default");

-- Table: rohbot.notifications

-- DROP TABLE rohbot.notifications;

CREATE TABLE rohbot.notifications
(
  id bigserial NOT NULL,
  userid bigint NOT NULL,
  regex text NOT NULL,
  devicetoken text NOT NULL,
  CONSTRAINT notifications_pkey PRIMARY KEY (id),
  CONSTRAINT notifications_userid_fkey FOREIGN KEY (userid)
      REFERENCES rohbot.accounts (id) MATCH SIMPLE
      ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT notifications_devicetoken_key UNIQUE (devicetoken)
)
WITH (
  OIDS=FALSE
);
ALTER TABLE rohbot.notifications
  OWNER TO server_group;

-- Index: rohbot.notifications_devicetoken_idx

-- DROP INDEX rohbot.notifications_devicetoken_idx;

CREATE UNIQUE INDEX notifications_devicetoken_idx
  ON rohbot.notifications
  USING btree
  (devicetoken COLLATE pg_catalog."default");

-- Index: rohbot.notifications_userid_idx

-- DROP INDEX rohbot.notifications_userid_idx;

CREATE INDEX notifications_userid_idx
  ON rohbot.notifications
  USING btree
  (userid);

