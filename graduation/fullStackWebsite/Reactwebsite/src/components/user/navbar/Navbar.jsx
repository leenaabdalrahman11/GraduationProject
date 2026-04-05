import React, { useEffect, useState } from "react";
import { Link, useNavigate, useLocation } from "react-router-dom";
import style from "./Navbar.module.css";

export default function Navbar() {
  const [visible, setVisible] = useState(true);
  const [lastScroll, setLastScroll] = useState(0);
  const [isOpen, setIsOpen] = useState(false);
  const [token, setToken] = useState(localStorage.getItem("token"));
  const navigate = useNavigate();
  const location = useLocation();

  useEffect(() => {
    const handleScroll = () => {
      const currentScroll = window.scrollY;

      setVisible(currentScroll < lastScroll);
      setLastScroll(currentScroll);
    };

    window.addEventListener("scroll", handleScroll);
    return () => window.removeEventListener("scroll", handleScroll);
  }, [lastScroll]);
  useEffect(() => {
    setToken(localStorage.getItem("token"));
  }, [location.pathname]);
  function handleLogout() {
    localStorage.removeItem("token");
    localStorage.removeItem("user");
    navigate("/login");
  }
  return (
    <>
      <nav
        className={`navbar navbar-expand-lg bg-body-tertiary w-full ${style.navbar} ${visible ? style.show : style.hide}`}
      >
        <div
          className={` flex justify-between items-center ${style.navbarContainer}`}
        >
          <div className="w-full flex items-center justify-between py-4 px-4">
            <div className=" flex justify-start">
              <Link
                to="/search"
                className="text-[#bc4c2a] text-lg hover:opacity-70 transition"
              >
                <i className="fa-solid fa-magnifying-glass"></i>
              </Link>
            </div>
            <div className="w-1/3"></div>

            <div className="w-1/3 flex justify-center">
              <Link
                to="/"
                className={`${style.logo} text-[#bc4c2a] text-5xl italic`}
              >
                DevHub.
              </Link>
            </div>

            <div className="w-1/3 flex justify-end items-center gap-8">
              {token ? (
                <button
                  className="!text-[#bc4c2a] text-lg hover:opacity-70 transition no-underline"
                  onClick={handleLogout}
                >
                  Log Out
                </button>
              ) : (
                <Link to="/login">Log In</Link>
              )}
              <Link
                to="/cart"
                className="text-[#bc4c2a] text-lg hover:opacity-70 transition no-underline"
              >
                Cart (0)
              </Link>
            </div>
          </div>
          <div
            className={`${isOpen ? "d-block" : "d-none"} w-[100%] d-lg-flex `}
          >
            <ul className="navbar-nav me-auto mb-2 mb-lg-0  w-[100%] flex justify-center ">
              <li className="nav-item">
                <Link className="nav-link active  text-center" to="/">
                  Shop All
                </Link>
              </li>

              <li className="nav-item text-center">
                <Link className="nav-link" to="/ourStory">
                  Our Story
                </Link>
              </li>

              <li className="nav-item text-center">
                <Link className="nav-link" to="/ourCraft">
                  Our Craft
                </Link>
              </li>

              <li className="nav-item text-center">
                <Link className="nav-link" to="/contact">
                  Contact
                </Link>
              </li>
            </ul>
          </div>
        </div>
      </nav>
    </>
  );
}
