CREATE TABLE IF NOT EXISTS test_check_log (
    id INT AUTO_INCREMENT PRIMARY KEY,
    page_name VARCHAR(200) NOT NULL,
    button_name VARCHAR(100) NOT NULL,
    action_type VARCHAR(20) NOT NULL,
    request_url VARCHAR(500) NULL,
    request_body TEXT NULL,
    response_status INT NULL,
    db_check_sql TEXT NULL,
    db_expected_change VARCHAR(50) NULL,
    db_actual_result TEXT NULL,
    passed TINYINT(1) NOT NULL DEFAULT 0,
    fix_attempts INT NOT NULL DEFAULT 0,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);
