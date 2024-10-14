START TRANSACTION;

CREATE TABLE "CnpjEstoque" (
    "Id" uuid NOT NULL,
    "DocCedente" text NOT NULL,
    "DocSacado" text NOT NULL,
    CONSTRAINT "PK_CnpjEstoque" PRIMARY KEY ("Id")
);

CREATE TABLE "RetornoReceita" (
    "Id" uuid NOT NULL,
    ultima_atualizacao timestamp with time zone NOT NULL,
    cnpj text NOT NULL,
    tipo text NOT NULL,
    porte text NOT NULL,
    nome text NOT NULL,
    fantasia text NOT NULL,
    abertura text NOT NULL,
    natureza_juridica text NOT NULL,
    logradouro text NOT NULL,
    numero text NOT NULL,
    complemento text NOT NULL,
    cep text NOT NULL,
    bairro text NOT NULL,
    municipio text NOT NULL,
    uf text NOT NULL,
    email text NOT NULL,
    telefone text NOT NULL,
    efr text NOT NULL,
    situacao text NOT NULL,
    CONSTRAINT "PK_RetornoReceita" PRIMARY KEY ("Id")
);

COMMIT;

START TRANSACTION;

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

INSERT INTO
    "CnpjEstoque" (
        "Id",
        "DocCedente",
        "DocSacado"
    )
VALUES (
        uuid_generate_v4 (),
        '01944765000142',
        '01944765000142'
    ),
    (
        uuid_generate_v4 (),
        '02482618000160',
        '02482618000160'
    ),
    (
        uuid_generate_v4 (),
        '07081898000119',
        '07081898000119'
    ),
    (
        uuid_generate_v4 (),
        '07348672000131',
        '07348672000131'
    ),
    (
        uuid_generate_v4 (),
        '07900208007704',
        '07900208007704'
    ),
    (
        uuid_generate_v4 (),
        '08418947000129',
        '08418947000129'
    ),
    (
        uuid_generate_v4 (),
        '08542491000104',
        '08542491000104'
    ),
    (
        uuid_generate_v4 (),
        '08561701000101',
        '08561701000101'
    ),
    (
        uuid_generate_v4 (),
        '10337124000110',
        '10337124000110'
    ),
    (
        uuid_generate_v4 (),
        '10735950000118',
        '10735950000118'
    ),
    (
        uuid_generate_v4 (),
        '10757237013900',
        '10757237013900'
    ),
    (
        uuid_generate_v4 (),
        '13013655000227',
        '13013655000227'
    ),
    (
        uuid_generate_v4 (),
        '18727053000174',
        '18727053000174'
    ),
    (
        uuid_generate_v4 (),
        '21777937000148',
        '21777937000148'
    ),
    (
        uuid_generate_v4 (),
        '22682915000167',
        '22682915000167'
    ),
    (
        uuid_generate_v4 (),
        '24049282000180',
        '24049282000180'
    ),
    (
        uuid_generate_v4 (),
        '24217653000357',
        '24217653000357'
    ),
    (
        uuid_generate_v4 (),
        '26481429000131',
        '26481429000131'
    ),
    (
        uuid_generate_v4 (),
        '29233652000158',
        '29233652000158'
    ),
    (
        uuid_generate_v4 (),
        '31629820000170',
        '31629820000170'
    );

COMMIT;