create table zuora_access_export
(
    id             bigint not null default nextval('zuora_access_export_id_seq'),
    version        bigint,
    timestamp      text,
    ssoid          text,
    account_number text,
    product_name   text
);
